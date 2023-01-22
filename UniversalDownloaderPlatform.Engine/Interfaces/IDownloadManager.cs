using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Events;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Engine.Interfaces
{
    internal interface IDownloadManager
    {
        /// <summary>
        /// Initialization function, called on every UniversalDownloader.Download call
        /// </summary>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);
        event EventHandler<FileDownloadedEventArgs> FileDownloaded;

        Task Download(List<ICrawledUrl> crawledUrls, CancellationToken cancellationToken);
    }
}
