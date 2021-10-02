using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Events;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface IPageCrawler
    {
        event EventHandler<PostCrawlEventArgs> PostCrawlStart;
        event EventHandler<PostCrawlEventArgs> PostCrawlEnd;
        event EventHandler<NewCrawledUrlEventArgs> NewCrawledUrl;
        event EventHandler<CrawlerMessageEventArgs> CrawlerMessage;
        /// <summary>
        /// Initialization function, called on every PatreonDownloader.Download call
        /// </summary>
        /// <returns></returns>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);
        Task<List<ICrawledUrl>> Crawl(ICrawlTargetInfo crawlTargetInfo, string downloadDirectory);
    }
}
