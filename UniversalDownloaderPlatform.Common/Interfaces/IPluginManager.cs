using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface IPluginManager
    {
        /// <summary>
        /// Initialization function, called on every PatreonDownloader.Download call
        /// </summary>
        /// <returns></returns>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);

        /// <summary>
        /// Download file using one of the registered plugins (or default if none are found)
        /// </summary>
        /// <param name="crawledUrl"></param>
        /// <param name="downloadDirectory"></param>
        /// <returns></returns>
        Task DownloadCrawledUrl(ICrawledUrl crawledUrl, string downloadDirectory);

        /// <summary>
        /// Run entry contents through every plugin to extract supported urls
        /// </summary>
        /// <param name="htmlContents"></param>
        /// <returns></returns>
        Task<List<string>> ExtractSupportedUrls(string htmlContents);
    }
}
