using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    public class RemoteFileSizeChecker : IRemoteFileSizeChecker
    {
        private HttpClient _httpClient;

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private int _maxRetries;
        private int _retryMultiplier;
        private readonly Version _httpVersion = HttpVersion.Version20;

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _maxRetries = settings.MaxDownloadRetries;
            _retryMultiplier = settings.RetryMultiplier;

            HttpClientHandler httpClientHandler = new HttpClientHandler();
            if (settings.CookieContainer != null)
            {
                httpClientHandler.UseCookies = true;
                httpClientHandler.CookieContainer = settings.CookieContainer;
            }

            _httpClient = new HttpClient(httpClientHandler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        }

        public async Task<long> GetRemoteFileSize(string url)
        {
            return await GetRemoteFileSizeInternal(url);
        }

        private async Task<long> GetRemoteFileSizeInternal(string url, int retry = 0, int retryTooManyRequests = 0)
        {
            if (retry > 0)
            {
                if (retry >= _maxRetries)
                {
                    throw new Exception("Retries limit reached");
                }

                await Task.Delay(retry * _retryMultiplier * 1000);
            }

            if (retryTooManyRequests > 0)
                await Task.Delay(retryTooManyRequests * _retryMultiplier * 1000);

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, url) { Version = _httpVersion })
                {
                    using (HttpResponseMessage responseMessage =
                        await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
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
                                    throw new WebException(
                                        $"[Remote size check] Unable to get remote file size as status code is {responseMessage.StatusCode}");
                                case HttpStatusCode.Moved:
                                    string newLocation = responseMessage.Headers.Location.ToString();
                                    _logger.Debug(
                                        $"[Remote size check] {url} has been moved to: {newLocation}, retrying using new url");
                                    return await GetRemoteFileSizeInternal(newLocation);
                                case HttpStatusCode.TooManyRequests:
                                    retryTooManyRequests++;
                                    _logger.Debug($"[Remote size check] Too many requests for {url}, waiting for {retryTooManyRequests * _retryMultiplier} seconds...");
                                    return await GetRemoteFileSizeInternal(url, 0, retryTooManyRequests);
                            }

                            retry++;

                            _logger.Debug(
                                $"Remote file size check: {url} returned status code {responseMessage.StatusCode}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)...");
                            return await GetRemoteFileSizeInternal(url, retry);
                        }

                        return responseMessage.Content.Headers.ContentLength ?? 0;
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                retry++;
                _logger.Debug(ex, $"Encountered error while trying to download {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileSizeInternal(url, retry);
            }
            catch (IOException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered IO error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileSizeInternal(url, retry);
            }
            catch (SocketException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered connection error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileSizeInternal(url, retry);
            }
            catch (Exception ex)
            {
                throw new WebException($"Unable to download from {url}: {ex.Message}", ex);
            }
        }
    }
}
