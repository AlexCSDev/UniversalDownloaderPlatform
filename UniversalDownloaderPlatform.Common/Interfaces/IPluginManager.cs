using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface IPluginManager
    {
        /// <summary>
        /// Initialization function, called on every UniversalDownloader.Download call
        /// </summary>
        /// <returns></returns>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);

        /// <summary>
        /// Download file using one of the registered plugins (or default if none are found)
        /// </summary>
        /// <param name="crawledUrl"></param>
        /// <returns></returns>
        Task DownloadCrawledUrl(ICrawledUrl crawledUrl);

        /// <summary>
        /// Run entry contents through every plugin to extract supported urls.
        /// </summary>
        /// <param name="htmlContents"></param>
        /// <returns>List of extracted urls. If htmlContents is empty or null returns empty list.</returns>
        Task<List<string>> ExtractSupportedUrls(string htmlContents);

        /// <summary>
        /// Run crawled url through every plugin to determine if any of the plugins want to process this url and to have minimal processing of it by ICrawledUrlProcessor
        /// </summary>
        /// <param name="crawledUrl"></param>
        /// <returns></returns>
        Task ProcessCrawledUrl(ICrawledUrl crawledUrl);
    }
}
