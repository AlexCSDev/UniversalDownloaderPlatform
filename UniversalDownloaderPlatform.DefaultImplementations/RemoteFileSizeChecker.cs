using System;
using System.Net;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    public class RemoteFileSizeChecker : IRemoteFileSizeChecker
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public async Task<long> GetRemoteFileSize(string url)
        {
            return await GetRemoteFileSizeInternal(url);
        }

        private async Task<long> GetRemoteFileSizeInternal(string url, int retry = 0)
        {
            if (retry >= 5)
                throw new Exception($"Retry limit reached");

            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Method = "HEAD";

            using (HttpWebResponse webResponse = (HttpWebResponse)(await webRequest.GetResponseAsync()))
            {
                if (!IsSuccessStatusCode(webResponse.StatusCode))
                {
                    switch (webResponse.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                        case HttpStatusCode.Forbidden:
                        case HttpStatusCode.NotFound:
                        case HttpStatusCode.MethodNotAllowed:
                        case HttpStatusCode.Gone:
                            throw new WebException($"Unable to get remote file size as status code is {webResponse.StatusCode}");
                    }

                    _logger.Debug($"Remote file size check: {url} returned status code {webResponse.StatusCode}, retrying ({4 - retry} retries left)...");
                    return await GetRemoteFileSizeInternal(url, retry + 1);
                }

                string fileSize = webResponse.Headers.Get("Content-Length");

                return Convert.ToInt64(fileSize);
            }
        }

        private bool IsSuccessStatusCode(HttpStatusCode statusCode)
        {
            return (int)statusCode >= 200 && (int)statusCode <= 299;
        }
    }
}
