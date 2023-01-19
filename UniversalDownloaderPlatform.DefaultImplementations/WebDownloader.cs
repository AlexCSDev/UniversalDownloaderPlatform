using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Helpers;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    //TODO: Make disposable?
    public class WebDownloader : IWebDownloader
    {
        protected HttpClient _httpClient;
        protected HttpClientHandler _httpClientHandler;

        protected readonly IRemoteFileSizeChecker _remoteFileSizeChecker;
        protected readonly ICaptchaSolver _captchaSolver;

        private readonly SemaphoreSlim _captchaSemaphore;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        protected int _maxRetries;
        protected int _retryMultiplier;
        protected FileExistsAction _fileExistsAction;
        protected bool _isCheckRemoteFileSize;

        protected readonly Version _httpVersion = HttpVersion.Version20;

        public WebDownloader(IRemoteFileSizeChecker remoteFileSizeChecker, ICaptchaSolver captchaSolver)
        {
            _remoteFileSizeChecker = remoteFileSizeChecker;
            _captchaSolver = captchaSolver;

            _captchaSemaphore = new SemaphoreSlim(1, 1);
        }

        public virtual async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _maxRetries = settings.MaxDownloadRetries;
            _retryMultiplier = settings.RetryMultiplier;
            _fileExistsAction = settings.FileExistsAction;
            _isCheckRemoteFileSize = settings.IsCheckRemoteFileSize;

            _httpClientHandler = new HttpClientHandler();
            if (settings.CookieContainer != null)
            {
                _httpClientHandler.UseCookies = true;
                _httpClientHandler.CookieContainer = settings.CookieContainer;
                if(settings.CookieContainer.PerDomainCapacity == CookieContainer.DefaultPerDomainCookieLimit || settings.CookieContainer.Capacity == CookieContainer.DefaultCookieLimit)
                    _logger.Warn("[Developer warning] CookieContainer passed in IUniversalDownloaderPlatformSettings uses default cookie capacity settings. This might result in incorrect cookie management behavior. Consider tuning DefaultPerDomainCookieLimit and Capacity values if you encounter issues.");
            }

            if (!string.IsNullOrWhiteSpace(settings.ProxyServerAddress))
                _httpClientHandler.Proxy = new WebProxy(settings.ProxyServerAddress);

            _httpClient = new HttpClient(_httpClientHandler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);

            await _remoteFileSizeChecker.BeforeStart(settings);
            await _captchaSolver.BeforeStart(settings);
        }

        public virtual async Task DownloadFile(string url, string path, string refererUrl = null)
        {
            if(string.IsNullOrEmpty(url))
                throw new ArgumentException("Argument cannot be null or empty", nameof(url));
            if(string.IsNullOrEmpty(path))
                throw new ArgumentException("Argument cannot be null or empty", nameof(path));

            await DownloadFileInternal(url, path, refererUrl);
        }

        public virtual void UpdateCookies(CookieCollection cookieCollection)
        {
            if (!_httpClientHandler.UseCookies)
                throw new InvalidOperationException("Cookies are not enabled");

            foreach (Cookie cookie in cookieCollection)
            {
                _httpClientHandler.CookieContainer.Add(cookie);
            }
        }

        private async Task DownloadFileInternal(string url, string path, string refererUrl, int retry = 0, int retryTooManyRequests = 0)
        {
            //Warn: path is not being checked to be a valid path here

            string temporaryFilePath = $"{path}.dwnldtmp";

            try
            {
                if (File.Exists(temporaryFilePath))
                    File.Delete(temporaryFilePath);
            }
            catch (Exception fileDeleteException)
            {
                throw new DownloadException($"Unable to delete existing temporary file {temporaryFilePath}", fileDeleteException);
            }

            if (retry > 0)
            {
                if (retry >= _maxRetries)
                {
                    throw new DownloadException("Retries limit reached");
                }

                await Task.Delay(retry * _retryMultiplier * 1000);
            }

            if(retryTooManyRequests > 0)
                await Task.Delay(retryTooManyRequests * _retryMultiplier * 1000);

            long remoteFileSize = -1;
            try
            {
                remoteFileSize = await _remoteFileSizeChecker.GetRemoteFileSize(url);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to retrieve remote file size, size check will be skipped: {ex}");
            }

            if (File.Exists(path))
            {
                if (!FileExistsActionHelper.DoFileExistsActionBeforeDownload(path, remoteFileSize, _isCheckRemoteFileSize, _fileExistsAction, LoggingFunction))
                    return;
            }

            try
            {
                //warning: returns '' in drive's root
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(new FileInfo(path).DirectoryName);
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to create directory for file {path}", ex);
            }

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url) {Version = _httpVersion})
                {
                    if (!string.IsNullOrWhiteSpace(refererUrl))
                    {
                        try
                        {
                            request.Headers.Referrer = new Uri(refererUrl);
                        }
                        catch (UriFormatException ex)
                        {
                            _logger.Error(ex, $"Invalid referer url: {refererUrl}. Error: {ex}");
                        }
                    }

                    using (HttpResponseMessage responseMessage =
                        await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (await RunCaptchaCheck(url, refererUrl, responseMessage))
                        {
                            await DownloadFileInternal(url, path, refererUrl, retry, retryTooManyRequests); //increase retry counter?
                            return;
                        }

                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            switch (responseMessage.StatusCode)
                            {
                                //todo: configure via config?
                                case HttpStatusCode.BadRequest:
                                case HttpStatusCode.Unauthorized:
                                case HttpStatusCode.Forbidden:
                                case HttpStatusCode.NotFound:
                                case HttpStatusCode.MethodNotAllowed:
                                case HttpStatusCode.Gone:
                                    throw new DownloadException(
                                        $"Error status code returned: {responseMessage.StatusCode}",
                                        responseMessage.StatusCode, await responseMessage.Content.ReadAsStringAsync());
                                case HttpStatusCode.Moved:
                                case HttpStatusCode.Found:
                                case HttpStatusCode.SeeOther:
                                case HttpStatusCode.TemporaryRedirect:
                                case HttpStatusCode.PermanentRedirect:
                                    string newLocation = responseMessage.Headers.Location.ToString();
                                    _logger.Debug(
                                        $"{url} has been moved to: {newLocation}, retrying using new url");
                                    await DownloadFileInternal(newLocation, refererUrl, path);
                                    return;
                                case HttpStatusCode.TooManyRequests:
                                    retryTooManyRequests++;
                                    _logger.Debug(
                                        $"Too many requests for {url}, waiting for {retryTooManyRequests * _retryMultiplier} seconds...");
                                    await DownloadFileInternal(url, path, refererUrl, 0, retryTooManyRequests);
                                    return;
                            }

                            retry++;

                            _logger.Debug(
                                $"{url} returned status code {responseMessage.StatusCode}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)...");
                            await DownloadFileInternal(url, path, refererUrl, retry);
                            return;
                        }

                        _logger.Debug($"Starting download: {url}");
                        using (Stream contentStream = await responseMessage.Content.ReadAsStreamAsync(),
                            stream = new FileStream(temporaryFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096,
                                true))
                        {
                            await contentStream.CopyToAsync(stream, 4096);
                        }

                        _logger.Debug($"Finished download: {url}");

                        FileInfo fileInfo = new FileInfo(temporaryFilePath);
                        long fileSize = fileInfo.Length;
                        fileInfo = null;

                        if (remoteFileSize > 0 && fileSize != remoteFileSize)
                        {
                            _logger.Warn(
                                $"Downloaded file size differs from the size returned by server. Local size: {fileSize}, remote size: {remoteFileSize}. File {url} will be redownloaded.");

                            File.Delete(temporaryFilePath);

                            retry++;

                            await DownloadFileInternal(url, path, refererUrl, retry);
                            return;
                        }

                        _logger.Debug($"File size check passed for: {url}");

                        _logger.Debug($"Renaming temporary file for: {url}");

                        try
                        {
                            FileExistsActionHelper.DoFileExistsActionAfterDownload(path, temporaryFilePath, _fileExistsAction, LoggingFunction);
                        }
                        catch(Exception ex)
                        {
                            throw new DownloadException(ex.Message, ex);
                        }
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered error while trying to download {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                await DownloadFileInternal(url, path, refererUrl, retry);
            }
            catch (IOException ex) when (!(ex is DirectoryNotFoundException))
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered IO error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                await DownloadFileInternal(url, path, refererUrl, retry);
            }
            catch (SocketException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered connection error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                await DownloadFileInternal(url, path, refererUrl, retry);
            }
            catch (DownloadException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to download from {url}: {ex.Message}", ex);
            }
        }

        public virtual async Task<string> DownloadString(string url, string refererUrl = null)
        {
            if(string.IsNullOrEmpty(url))
                throw new ArgumentException("Argument cannot be null or empty", nameof(url));

            return await DownloadStringInternal(url, refererUrl);
        }

        private async Task<string> DownloadStringInternal(string url, string refererUrl, int retry = 0, int retryTooManyRequests = 0)
        {
            if (retry > 0)
            {
                if (retry >= _maxRetries)
                {
                    throw new DownloadException("Retries limit reached");
                }

                await Task.Delay(retry * _retryMultiplier * 1000);
            }

            if (retryTooManyRequests > 0)
                await Task.Delay(retryTooManyRequests * _retryMultiplier * 1000);

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url) {Version = _httpVersion})
                {
                    //Add some additional headers to better mimic a real browser
                    request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                    request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
                    request.Headers.Add("Cache-Control", "no-cache");
                    request.Headers.Add("DNT", "1");

                    if (!string.IsNullOrWhiteSpace(refererUrl))
                    {
                        try
                        {
                            request.Headers.Referrer = new Uri(refererUrl);
                        }
                        catch (UriFormatException ex)
                        {
                            _logger.Error($"Invalid referer url: {refererUrl}");
                        }
                    }

                    using (HttpResponseMessage responseMessage =
                        await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if(await RunCaptchaCheck(url, refererUrl, responseMessage))
                            return await DownloadStringInternal(url, refererUrl, retry, retryTooManyRequests); //increase retry counter?

                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            switch (responseMessage.StatusCode)
                            {
                                case HttpStatusCode.BadRequest:
                                case HttpStatusCode.Unauthorized:
                                case HttpStatusCode.Forbidden:
                                case HttpStatusCode.NotFound:
                                case HttpStatusCode.MethodNotAllowed:
                                case HttpStatusCode.Gone:
                                    throw new DownloadException($"Error status code returned: {responseMessage.StatusCode}", 
                                        responseMessage.StatusCode, await responseMessage.Content.ReadAsStringAsync());
                                case HttpStatusCode.Moved:
                                case HttpStatusCode.Found:
                                case HttpStatusCode.SeeOther:
                                case HttpStatusCode.TemporaryRedirect:
                                case HttpStatusCode.PermanentRedirect:
                                    string newLocation = responseMessage.Headers.Location.ToString();
                                    _logger.Debug(
                                        $"{url} has been moved to: {newLocation}, retrying using new url");
                                    return await DownloadStringInternal(newLocation, refererUrl);
                                case HttpStatusCode.TooManyRequests:
                                    retryTooManyRequests++;
                                    _logger.Debug(
                                        $"Too many requests for {url}, waiting for {retryTooManyRequests * _retryMultiplier} seconds...");
                                    return await DownloadStringInternal(url, refererUrl, 0, retryTooManyRequests);
                            }

                            retry++;

                            _logger.Debug(
                                $"{url} returned status code {responseMessage.StatusCode}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)...");
                            return await DownloadStringInternal(url, refererUrl, retry);
                        }

                        return await responseMessage.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered timeout error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await DownloadStringInternal(url, refererUrl, retry);
            }
            catch (IOException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered IO error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await DownloadStringInternal(url, refererUrl, retry);
            }
            catch (SocketException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered connection error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await DownloadStringInternal(url, refererUrl, retry);
            }
            catch (DownloadException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to retrieve data from {url}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Check if captcha was triggered. True means the captcha was triggered and cookies were updated successfully, false means there were no captcha. If captcha could not be solved exception will be thrown.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="refererUrl"></param>
        /// <param name="responseMessage"></param>
        /// <returns></returns>
        protected virtual async Task<bool> RunCaptchaCheck(string url, string refererUrl, HttpResponseMessage responseMessage)
        {
            if(await _captchaSolver.IsCaptchaTriggered(responseMessage))
            {
                _logger.Trace($"[RunCaptchaCheck] Captcha found: {url}");
                try
                {
                    //Since downloading is multi-threaded we need to make sure another thread didn't solve the captcha already
                    bool enteredImmediatelly = _captchaSemaphore.Wait(0);
                    _logger.Trace($"[RunCaptchaCheck] enteredImmediatelly: {enteredImmediatelly}");

                    bool needToSolveCaptcha = true;

                    //Only recheck if we had to wait for another thread to unlock semaphore
                    if(!enteredImmediatelly)
                    {
                        await _captchaSemaphore.WaitAsync();
                        using (var request = new HttpRequestMessage(HttpMethod.Get, url) { Version = _httpVersion })
                        {
                            if (!string.IsNullOrWhiteSpace(refererUrl))
                            {
                                try
                                {
                                    request.Headers.Referrer = new Uri(refererUrl);
                                }
                                catch (UriFormatException ex)
                                {
                                    _logger.Error(ex, $"Invalid referer url: {refererUrl}. Error: {ex}");
                                }
                            }

                            using (responseMessage =
                                await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                            {
                                if (!await _captchaSolver.IsCaptchaTriggered(responseMessage))
                                    needToSolveCaptcha = false;
                            }
                        }
                    }

                    if(needToSolveCaptcha)
                    {
                        _logger.Trace($"[RunCaptchaCheck] Solving captcha: {url}");
                        CookieCollection cookieCollection = await _captchaSolver.SolveCaptcha(url);

                        if (cookieCollection == null)
                            throw new Exception($"Unable to solve captcha for url: {url}");

                        UpdateCookies(cookieCollection);
                    }
                    else
                        _logger.Trace($"[RunCaptchaCheck] No need to solve captcha anymore: {url}");
                }
                finally
                {
                    _captchaSemaphore.Release();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Logging function for FileExistsActionHelper calls
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        private void LoggingFunction(LogMessageLevel level, string message, Exception exception)
        {
            switch (level)
            {
                case LogMessageLevel.Trace:
                    _logger.Trace(message, exception);
                    break;
                case LogMessageLevel.Debug: 
                    _logger.Debug(message, exception); 
                    break;
                case LogMessageLevel.Fatal: 
                    _logger.Fatal(message, exception); 
                    break;
                case LogMessageLevel.Error: 
                    _logger.Error(message, exception); 
                    break;
                case LogMessageLevel.Warning: 
                    _logger.Warn(message, exception); 
                    break;
                case LogMessageLevel.Information: 
                    _logger.Info(message, exception); 
                    break;
            }
        }
    }
}
