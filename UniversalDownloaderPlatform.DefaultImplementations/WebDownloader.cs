using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    //TODO: Make disposable?
    public class WebDownloader : IWebDownloader
    {
        private HttpClient _httpClient;
        private readonly IRemoteFileSizeChecker _remoteFileSizeChecker;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public WebDownloader(IRemoteFileSizeChecker remoteFileSizeChecker, IUniversalDownloaderPlatformSettings settings)
        {
            _remoteFileSizeChecker =
                remoteFileSizeChecker ?? throw new ArgumentNullException(nameof(remoteFileSizeChecker));
        }

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            if (settings.CookieContainer != null)
            {
                httpClientHandler.UseCookies = true;
                httpClientHandler.CookieContainer = settings.CookieContainer;
            }

            _httpClient = new HttpClient(httpClientHandler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        }

        public virtual async Task DownloadFile(string url, string path, bool overwrite = false)
        {
            if(string.IsNullOrEmpty(url))
                throw new ArgumentException("Argument cannot be null or empty", nameof(url));
            if(string.IsNullOrEmpty(path))
                throw new ArgumentException("Argument cannot be null or empty", nameof(path));

            await DownloadFileInternal(url, path, overwrite);
        }

        private async Task DownloadFileInternal(string url, string path, bool overwrite, int retry = 0)
        {
            if (retry > 0)
            {
                if (retry >= 5)
                {
                    throw new Exception("Retries limit reached");
                }

                await Task.Delay(retry * 2 * 1000);
            }

            long remoteFileSize = -1;
            bool isFilesIdentical = false;
            try
            {
                remoteFileSize = await _remoteFileSizeChecker.GetRemoteFileSize(url);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Unable to retrieve remote file size, size check will be skipped: {ex}");
                isFilesIdentical = true;
            }

            if (File.Exists(path))
            {
                if (remoteFileSize > 0)
                {
                    _logger.Debug($"File {path} exists, size will be checked");
                    try
                    {
                        FileInfo fileInfo = new FileInfo(path);
                        long fileSize = fileInfo.Length;

                        if (fileSize != remoteFileSize)
                        {
                            string backupFilename =
                                $"{Path.GetFileNameWithoutExtension(path)}_old_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{Path.GetExtension(path)}";
                            _logger.Warn($"Local and remote file sizes does not match, file {url} will be redownloaded. Old file will be backed up as {backupFilename}. Remote file size: {remoteFileSize}, local file size: {fileSize}");
                            File.Move(path, Path.Combine(fileInfo.DirectoryName, backupFilename));
                        }
                        else
                        {
                            _logger.Debug($"File size for {path} matches");
                            isFilesIdentical = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Error during file comparison: {ex}");
                        isFilesIdentical = true; //we assume that local file is identical if we can't check remote file size
                    }
                }

                if (!isFilesIdentical && !overwrite)
                    throw new DownloadException($"File {path} already exists");
                else if (isFilesIdentical && !overwrite)
                {
                    _logger.Warn($"File {path} already exists and has the same file size as remote file (or remote file is not available). Skipping...");
                    return;
                }

                _logger.Warn($"File {path} already exists, will be overwriten!");
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
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    using (HttpResponseMessage responseMessage =
                        await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            switch (responseMessage.StatusCode)
                            {
                                //todo: configure via config?
                                case HttpStatusCode.Unauthorized:
                                case HttpStatusCode.Forbidden:
                                case HttpStatusCode.NotFound:
                                case HttpStatusCode.MethodNotAllowed:
                                case HttpStatusCode.Gone:
                                    throw new WebException($"Error status code returned: {responseMessage.StatusCode}");
                            }

                            retry++;

                            _logger.Debug($"{url} returned status code {responseMessage.StatusCode}, retrying in {retry * 2} seconds ({5 - retry} retries left)...");
                            await DownloadFileInternal(url, path, overwrite, retry);
                            return;
                        }

                        _logger.Debug($"Starting download: {url}");
                        using (Stream contentStream = await responseMessage.Content.ReadAsStreamAsync(),
                            stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                        {
                            await contentStream.CopyToAsync(stream, 4096);
                        }
                        _logger.Debug($"Finished download: {url}");

                        FileInfo fileInfo = new FileInfo(path);
                        long fileSize = fileInfo.Length;
                        fileInfo = null;

                        if (remoteFileSize > 0 && fileSize != remoteFileSize)
                        {
                            _logger.Warn($"Downloaded file size differs from the size returned by server. Local size: {fileSize}, remote size: {remoteFileSize}. File {url} will be redownloaded.");
                            
                            File.Delete(path);

                            retry++;

                            await DownloadFileInternal(url, path, overwrite, retry);
                            return;
                        }
                        _logger.Debug($"File size check passed for: {url}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to download from {url}: {ex.Message}", ex);
            }
        }

        public virtual async Task<string> DownloadString(string url)
        {
            if(string.IsNullOrEmpty(url))
                throw new ArgumentException("Argument cannot be null or empty", nameof(url));

            return await DownloadStringInternal(url);
        }

        private async Task<string> DownloadStringInternal(string url, int retry = 0)
        {
            if (retry > 0)
            {
                if (retry >= 5)
                {
                    throw new Exception("Retries limit reached");
                }

                await Task.Delay(retry * 2 * 1000);
            }

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    using (HttpResponseMessage responseMessage =
                        await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            switch (responseMessage.StatusCode)
                            {
                                case HttpStatusCode.Unauthorized:
                                case HttpStatusCode.Forbidden:
                                case HttpStatusCode.NotFound:
                                case HttpStatusCode.MethodNotAllowed:
                                case HttpStatusCode.Gone:
                                    throw new WebException($"Error status code returned: {responseMessage.StatusCode}");
                            }

                            retry++;

                            _logger.Debug($"{url} returned status code {responseMessage.StatusCode}, retrying in {retry * 2} seconds ({5 - retry} retries left)...");
                            return await DownloadStringInternal(url, retry);
                        }

                        return await responseMessage.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to retrieve data from {url}: {ex.Message}", ex);
            }
        }
    }
}
