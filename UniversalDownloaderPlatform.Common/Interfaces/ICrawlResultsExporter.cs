using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface ICrawlResultsExporter
    {
        /// <summary>
        /// Generate file with crawl results inside the download directory
        /// </summary>
        /// <param name="crawlTargetInfo">Crawl target information</param>
        /// <param name="crawledUrls">List of crawled urls</param>
        /// <returns></returns>
        Task ExportCrawlResults(ICrawlTargetInfo crawlTargetInfo, List<ICrawledUrl> crawledUrls);
    }
}
