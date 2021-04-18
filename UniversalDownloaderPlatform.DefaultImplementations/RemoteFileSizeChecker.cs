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

        //TODO: cookies?
        private async Task<long> GetRemoteFileSizeInternal(string url, int retry = 0)
        {
            if (retry > 0)
            {
                if (retry >= 5)
                {
                    throw new Exception("Retries limit reached");
                }

                await Task.Delay(retry * 2 * 1000);
            }

            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Method = "HEAD";

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
                    HttpStatusCode statusCode = ((HttpWebResponse) ex.Response).StatusCode;
                    switch (((HttpWebResponse)ex.Response).StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                        case HttpStatusCode.Forbidden:
                        case HttpStatusCode.NotFound:
                        case HttpStatusCode.MethodNotAllowed:
                        case HttpStatusCode.Gone:
                            throw new WebException(
                                $"Unable to get remote file size as status code is {statusCode}");
                    }

                    retry++;

                    _logger.Debug(
                        $"Remote file size check: {url} returned status code {statusCode}, retrying in {retry * 2} seconds ({5 - retry} retries left)...");
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
