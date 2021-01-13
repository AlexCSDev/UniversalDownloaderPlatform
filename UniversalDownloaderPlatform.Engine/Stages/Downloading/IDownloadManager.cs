using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Events;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Engine.Stages.Downloading
{
    internal interface IDownloadManager
    {
        event EventHandler<FileDownloadedEventArgs> FileDownloaded;
        Task Download(List<ICrawledUrl> crawledUrls, string downloadDirectory, CancellationToken cancellationToken);
    }
}
