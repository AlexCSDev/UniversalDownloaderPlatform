using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface ICrawlResultsExporter
    {
        /// <summary>
        /// Initialization function, called on every UniversalDownloader.Download call. IUniversalDownloaderPlatformSettings.DownloadDirectory can be used here safely.
        /// </summary>
        /// <returns></returns>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);

        /// <summary>
        /// Generate file with crawl results inside the download directory
        /// </summary>
        /// <param name="crawlTargetInfo">Crawl target information</param>
        /// <param name="crawledUrls">List of crawled urls</param>
        /// <returns></returns>
        Task ExportCrawlResults(ICrawlTargetInfo crawlTargetInfo, List<ICrawledUrl> crawledUrls);
    }
}
