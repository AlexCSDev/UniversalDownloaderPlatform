using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.Common.Events;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Engine.Exceptions;
using UniversalDownloaderPlatform.Engine.Helpers;

namespace UniversalDownloaderPlatform.Engine.Stages.Downloading
{
    internal sealed class DownloadManager : IDownloadManager
    {
        private readonly IPluginManager _pluginManager;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<FileDownloadedEventArgs> FileDownloaded;

        public DownloadManager(IPluginManager pluginManager)
        {
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        }

        public async Task Download(List<ICrawledUrl> crawledUrls, string downloadDirectory, CancellationToken cancellationToken)
        {
            if(crawledUrls == null)
                throw new ArgumentNullException(nameof(crawledUrls));
            if(string.IsNullOrEmpty(downloadDirectory))
                throw new ArgumentException("Argument cannot be null or empty", nameof(downloadDirectory));

            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(4))
            {
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < crawledUrls.Count; i++)
                {
                    concurrencySemaphore.Wait();

                    cancellationToken.ThrowIfCancellationRequested();

                    int entryPos = i;
                    Task task = Task.Run(async () =>
                    {
                        try
                        {
                            ICrawledUrl entry = crawledUrls[entryPos];

                            if (!UrlChecker.IsValidUrl(entry.Url))
                            {
                                _logger.Error(
                                    $"Invalid or blacklisted url: {entry.Url}");
                                return;
                            }

                            _logger.Debug($"Downloading {entryPos + 1}/{crawledUrls.Count}: {entry.Url}");

                            try
                            {
                                await _pluginManager.DownloadCrawledUrl(entry, downloadDirectory);
                                OnFileDownloaded(new FileDownloadedEventArgs(entry.Url, crawledUrls.Count));
                            }
                            catch (DownloadException ex)
                            {
                                string logMessage = $"Error while downloading {entry.Url}: {ex.Message}";
                                if (ex.InnerException != null)
                                    logMessage += $". Inner Exception: {ex.InnerException}";
                                _logger.Error(logMessage);
                                OnFileDownloaded(new FileDownloadedEventArgs(entry.Url, crawledUrls.Count,
                                    false, logMessage));
                            }
                            catch (Exception ex)
                            {
                                throw new UniversalDownloaderPlatformException(
                                    $"Error while downloading {entry.Url}: {ex.Message}", ex);
                            }
                        }
                        finally
                        {
                            concurrencySemaphore.Release();
                        }
                    }, cancellationToken);

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
                _logger.Debug("Finished all tasks");
            }
        }

        private void OnFileDownloaded(FileDownloadedEventArgs e)
        {
            EventHandler<FileDownloadedEventArgs> handler = FileDownloaded;

            handler?.Invoke(this, e);
        }
    }
}
