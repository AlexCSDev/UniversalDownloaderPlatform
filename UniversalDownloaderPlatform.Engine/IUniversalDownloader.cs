using System;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Events;

namespace UniversalDownloaderPlatform.Engine
{
    internal interface IUniversalDownloader
    {
        event EventHandler<DownloaderStatusChangedEventArgs> StatusChanged;
        event EventHandler<PostCrawlEventArgs> PostCrawlStart;
        event EventHandler<PostCrawlEventArgs> PostCrawlEnd;
        event EventHandler<NewCrawledUrlEventArgs> NewCrawledUrl;
        event EventHandler<CrawlerMessageEventArgs> CrawlerMessage;
        event EventHandler<FileDownloadedEventArgs> FileDownloaded;
        Task Download(string url, PatreonDownloaderSettings settings);
    }
}
