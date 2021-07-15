using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    public class RemoteFileSizeChecker : IRemoteFileSizeChecker
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private CookieContainer _cookieContainer;
        private int _maxRetries;
        private int _retryMultiplier;
        private readonly Version _httpVersion = new Version(2, 0);

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _cookieContainer = settings.CookieContainer;
            _maxRetries = settings.MaxDownloadRetries;
            _retryMultiplier = settings.RetryMultiplier;
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

            HttpWebRequest webRequest = WebRequest.Create(url) as HttpWebRequest;
            webRequest.Method = "HEAD";
            webRequest.CookieContainer = _cookieContainer;
            webRequest.ProtocolVersion = _httpVersion;

            try
            {
                using (HttpWebResponse webResponse = (HttpWebResponse) (await webRequest.GetResponseAsync()))
                {
                    if (!IsSuccessStatusCode(webResponse.StatusCode))
                    {
                        //sanity check, this code should not be reached
                        retry++;

                        _logger.Fatal(
                            $"[UNREACHABLE CODE REACHED, NOTIFY DEVELOPER] Remote file size check: {url} returned status code {webResponse.StatusCode}, retrying in {retry * 2} seconds ({5 - retry} retries left)...");
                        return await GetRemoteFileSizeInternal(url, retry);
                    }

                    string fileSize = webResponse.Headers.Get("Content-Length");

                    return Convert.ToInt64(fileSize);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    HttpWebResponse response = (HttpWebResponse) ex.Response;
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.BadRequest:
                        case HttpStatusCode.Unauthorized:
                        case HttpStatusCode.Forbidden:
                        case HttpStatusCode.NotFound:
                        case HttpStatusCode.MethodNotAllowed:
                        case HttpStatusCode.Gone:
                            throw new WebException(
                                $"[Remote size check] Unable to get remote file size as status code is {response.StatusCode}");
                        case HttpStatusCode.Moved:
                            string newLocation = response.Headers["Location"];
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
                        $"Remote file size check: {url} returned status code {response.StatusCode}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)...");
                    return await GetRemoteFileSizeInternal(url, retry);
                }
            }

            return 0;
        }

        private bool IsSuccessStatusCode(HttpStatusCode statusCode)
        {
            return (int)statusCode >= 200 && (int)statusCode <= 299;
        }
    }
}
