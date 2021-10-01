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
using UniversalDownloaderPlatform.Engine.Interfaces;
using UniversalDownloaderPlatform.Common.Enums;

namespace UniversalDownloaderPlatform.Engine.Stages.Downloading
{
    internal sealed class DownloadManager : IDownloadManager
    {
        private readonly IPluginManager _pluginManager;
        private readonly ICrawledUrlProcessor _crawledUrlProcessor;
        private readonly IUrlChecker _urlChecker;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<FileDownloadedEventArgs> FileDownloaded;

        public DownloadManager(IPluginManager pluginManager, ICrawledUrlProcessor crawledUrlProcessor, IUrlChecker urlChecker)
        {
            _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
            _crawledUrlProcessor = crawledUrlProcessor ?? throw new ArgumentNullException(nameof(crawledUrlProcessor));
            _urlChecker = urlChecker ?? throw new ArgumentNullException(nameof(urlChecker));
        }

        public async Task Download(List<ICrawledUrl> crawledUrls, string downloadDirectory, IUniversalDownloaderPlatformSettings settings, CancellationToken cancellationToken)
        {
            if(crawledUrls == null)
                throw new ArgumentNullException(nameof(crawledUrls));
            if(string.IsNullOrEmpty(downloadDirectory))
                throw new ArgumentException("Argument cannot be null or empty", nameof(downloadDirectory));

            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(4)) //todo: allow setting the count here (issue #4)
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

                            if (!_urlChecker.IsValidUrl(entry.Url))
                            {
                                _logger.Error($"Invalid url: {entry.Url}");
                                return;
                            }

                            if (_urlChecker.IsBlacklistedUrl(entry.Url))
                            {
                                _logger.Warn($"Url is blacklisted: {entry.Url}");
                                return;
                            }

                            _logger.Debug($"Downloading {entryPos + 1}/{crawledUrls.Count}: {entry.Url}");

                            try
                            {
                                _logger.Debug($"Calling url processor for: {entry.Url}");
                                bool isDownloadAllowed = await _crawledUrlProcessor.ProcessCrawledUrl(entry, downloadDirectory, settings);

                                if (isDownloadAllowed)
                                {
                                    if (string.IsNullOrWhiteSpace(entry.DownloadPath))
                                        throw new DownloadException($"Download path is not filled for {entry.Url}");

                                    await _pluginManager.DownloadCrawledUrl(entry, downloadDirectory);
                                }
                                else
                                {
                                    _logger.Debug($"ProcessCrawledUrl returned false, {entry.Url} will be skipped");
                                }

                                //TODO: mark isDownloadAllowed = false entries as skipped
                                entry.IsDownloaded = true;
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
