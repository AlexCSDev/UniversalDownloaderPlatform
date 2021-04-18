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
        /// Download file
        /// </summary>
        /// <param name="url">File url</param>
        /// <param name="path">Path where the file should be saved</param>
        /// <param name="overwrite">Should it be allowed to overwrite file?</param>
        Task DownloadFile(string url, string path, bool overwrite = false);

        /// <summary>
        /// Download url as string data
        /// </summary>
        /// <param name="url">Url to download</param>
        /// <returns>String</returns>
        Task<string> DownloadString(string url);
    }
}
