using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface ICrawledUrlProcessor
    {
        /// <summary>
        /// Initialization function, called on every PatreonDownloader.Download call
        /// </summary>
        /// <returns></returns>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);
        /// <summary>
        /// Do any additional processing on the crawled url before download process starts. By returning false the function can skip downloading of this url.
        /// </summary>
        /// <param name="crawledUrl">Crawled url</param>
        /// <param name="downloadDirectory">Download directory</param>
        /// <returns></returns>
        Task<bool> ProcessCrawledUrl(ICrawledUrl crawledUrl, string downloadDirectory);
    }
}
