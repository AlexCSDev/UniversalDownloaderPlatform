using System.Net;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface IWebDownloader
    {
        /// <summary>
        /// Initialization function, called on every PatreonDownloader.Download call
        /// </summary>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);

        /// <summary>
        /// Add new cookies and replace existing ones
        /// </summary>
        /// <param name="cookieCollection"></param>
        void UpdateCookies(CookieCollection cookieCollection);

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="url">File url</param>
        /// <param name="path">Path where the file should be saved</param>
        /// <param name="refererUrl">Url to be placed into the referer header, can be null</param>
        Task DownloadFile(string url, string path, string refererUrl = null);

        /// <summary>
        /// Download url as string data
        /// </summary>
        /// <param name="url">Url to download</param>
        /// <param name="refererUrl">Url to be placed into the referer header, can be null</param>
        /// <returns>String</returns>
        Task<string> DownloadString(string url, string refererUrl = null);
    }
}
