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
        Task<List<ICrawledUrl>> Crawl(ICrawlTargetInfo crawlTargetInfo, /*PatreonDownloaderSettings settings,*/ string downloadDirectory);
    }
}
