using System;
using System.Net;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    public class RemoteFileSizeChecker : IRemoteFileSizeChecker
    {
        public async Task<long> GetRemoteFileSize(string url)
        {

            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Method = "HEAD";

            using (HttpWebResponse webResponse = (HttpWebResponse)(await webRequest.GetResponseAsync()))
            {
                if(webResponse.StatusCode != HttpStatusCode.OK)
                    throw new WebException($"Unable to get remote file size as status code is {webResponse.StatusCode}");

                string fileSize = webResponse.Headers.Get("Content-Length");

                return Convert.ToInt64(fileSize);
            }
        }
    }
}
