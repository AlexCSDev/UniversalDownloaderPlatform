using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ninject;
using Ninject.Modules;
using NLog;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.Engine.DependencyInjection;
using UniversalDownloaderPlatform.Common.Events;
using UniversalDownloaderPlatform.Engine.Exceptions;
using UniversalDownloaderPlatform.Engine.Stages.Downloading;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Engine.Interfaces;

namespace UniversalDownloaderPlatform.Engine
{
    public sealed class UniversalDownloader : IDisposable
    {
        private readonly IPluginManager _pluginManager;
        private readonly IDownloadManager _downloadManager;
        private readonly IPageCrawler _pageCrawler;
        private readonly ICrawlTargetInfoRetriever _crawlTargetInfoRetriever;
        private readonly IKernel _kernel;

        private readonly SemaphoreSlim _initializationSemaphore;
        private CancellationTokenSource _cancellationTokenSource;
        //We don't want those variables to be optimized by compiler
        private volatile bool _isRunning;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<DownloaderStatusChangedEventArgs> StatusChanged;
        public event EventHandler<PostCrawlEventArgs> PostCrawlStart;
        public event EventHandler<PostCrawlEventArgs> PostCrawlEnd;
        public event EventHandler<NewCrawledUrlEventArgs> NewCrawledUrl;
        public event EventHandler<CrawlerMessageEventArgs> CrawlerMessage;
        public event EventHandler<FileDownloadedEventArgs> FileDownloaded;

        public bool IsRunning
        {
            get => _isRunning;
        }

        // TODO: Implement cancellation token
        /// <summary>
        /// Create a new PatreonDownloader
        /// </summary>
        /// <param name="ninjectModule">Ninject module containing all required custom bindings (refer to documentation for details)</param>
        public UniversalDownloader(INinjectModule ninjectModule)
        {
            _initializationSemaphore = new SemaphoreSlim(1, 1);

            _logger.Debug($"Initializing PatreonDownloader...");

            _logger.Debug("Initializing ninject kernel");
            _kernel = new StandardKernel(new MainModule());

            _logger.Debug("Loading custom ninject module");
            _kernel.Load(ninjectModule);

            _logger.Debug("Loading ICrawlTargetInfoRetriever");
            _crawlTargetInfoRetriever = _kernel.Get<ICrawlTargetInfoRetriever>();
            //todo: check bindings

            _logger.Debug("Configuring IPageCrawler");
            _pageCrawler = _kernel.Get<IPageCrawler>(); //required for dispose
            _pageCrawler.PostCrawlStart += PageCrawlerOnPostCrawlStart;
            _pageCrawler.PostCrawlEnd += PageCrawlerOnPostCrawlEnd;
            _pageCrawler.NewCrawledUrl += PageCrawlerOnNewCrawledUrl;
            _pageCrawler.CrawlerMessage += PageCrawlerOnCrawlerMessage;

            _logger.Debug("Binding default IPlugin");
            //_kernel.Bind<IPlugin>().to.WhenInjectedInto<IPluginManager>();

            _logger.Debug("Initializing plugin manager");
            _pluginManager = _kernel.Get<IPluginManager>();

            _logger.Debug("Initializing download manager");
            _downloadManager = _kernel.Get<IDownloadManager>();
            _downloadManager.FileDownloaded += DownloadManagerOnFileDownloaded;

            OnStatusChanged(new DownloaderStatusChangedEventArgs(DownloaderStatus.Ready));
        }

        /// <summary>
        /// Download specified creator
        /// </summary>
        /// <param name="url">Url of the creator's page</param>
        /// <param name="settings">Downloader settings, will be set to default values if not provided</param>
        public async Task Download(string url)
        {
            if(string.IsNullOrEmpty(url))
                throw new ArgumentException("Argument cannot be null or empty", nameof(url));

            url = url.ToLower(CultureInfo.InvariantCulture);

            string downloadDirectory = null; //todo: allow passing download directory and other settings

            try
            {
                // Make sure several threads cannot access initialization code at once
                await _initializationSemaphore.WaitAsync();
                try
                {
                    if (_isRunning)
                    {
                        throw new DownloaderAlreadyRunningException(
                            "Unable to start new download while another one is in progress");
                    }

                    _isRunning = true;
                }
                finally
                {
                    // Release the lock after all required initialization code has finished execution
                    _initializationSemaphore.Release();
                }

                OnStatusChanged(new DownloaderStatusChangedEventArgs(DownloaderStatus.Initialization));

                _cancellationTokenSource = new CancellationTokenSource();

                //Call initialization code in all plugins
                await _pluginManager.BeforeStart(/*settings*/);

                //TODO: custom info retrieval call (campaign retrieval, cookie check, etc)

                ICrawlTargetInfo crawlTargetInfo = await _crawlTargetInfoRetriever.RetrieveCrawlTargetInfo(url);

                try
                {

                    if (string.IsNullOrEmpty(downloadDirectory))
                    {
                        downloadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "download", crawlTargetInfo.SaveDirectory);
                    }
                    if (!Directory.Exists(downloadDirectory))
                    {
                        Directory.CreateDirectory(downloadDirectory);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Fatal($"Unable to create download directory: {ex}");
                    throw new UniversalDownloaderPlatformException("Unable to create download directory", ex);
                }

                _logger.Debug("Starting crawler");
                OnStatusChanged(new DownloaderStatusChangedEventArgs(DownloaderStatus.Crawling));
                List<ICrawledUrl> crawledUrls = await _pageCrawler.Crawl(crawlTargetInfo/*, settings*/, downloadDirectory);

                /*_logger.Debug("Closing puppeteer browser");
                await _puppeteerEngine.CloseBrowser();*/

                _logger.Debug("Starting downloader");
                OnStatusChanged(new DownloaderStatusChangedEventArgs(DownloaderStatus.Downloading));
                await _downloadManager.Download(crawledUrls, downloadDirectory, _cancellationTokenSource.Token);

                _logger.Debug("Finished downloading");
                OnStatusChanged(new DownloaderStatusChangedEventArgs(DownloaderStatus.Done));
            }
            finally
            {
                _isRunning = false;
                OnStatusChanged(new DownloaderStatusChangedEventArgs(DownloaderStatus.Ready));
            }
        }

        private void PageCrawlerOnCrawlerMessage(object sender, CrawlerMessageEventArgs e)
        {
            EventHandler<CrawlerMessageEventArgs> handler = CrawlerMessage;
            handler?.Invoke(this, e);
        }

        private void PageCrawlerOnNewCrawledUrl(object sender, NewCrawledUrlEventArgs e)
        {
            EventHandler<NewCrawledUrlEventArgs> handler = NewCrawledUrl;
            handler?.Invoke(this, e);
        }

        private void PageCrawlerOnPostCrawlEnd(object sender, PostCrawlEventArgs e)
        {
            EventHandler<PostCrawlEventArgs> handler = PostCrawlEnd;
            handler?.Invoke(this, e);
        }

        private void PageCrawlerOnPostCrawlStart(object sender, PostCrawlEventArgs e)
        {
            EventHandler<PostCrawlEventArgs> handler = PostCrawlStart;
            handler?.Invoke(this, e);
        }

        private void DownloadManagerOnFileDownloaded(object sender, FileDownloadedEventArgs e)
        {
            EventHandler<FileDownloadedEventArgs> handler = FileDownloaded;
            handler?.Invoke(this, e);
        }

        private void OnStatusChanged(DownloaderStatusChangedEventArgs e)
        {
            EventHandler<DownloaderStatusChangedEventArgs> handler = StatusChanged;
            handler?.Invoke(this, e);
        }

        public void Dispose()
        {
            _pageCrawler.PostCrawlStart -= PageCrawlerOnPostCrawlStart;
            _pageCrawler.PostCrawlEnd -= PageCrawlerOnPostCrawlEnd;
            _pageCrawler.NewCrawledUrl -= PageCrawlerOnNewCrawledUrl;
            _pageCrawler.CrawlerMessage -= PageCrawlerOnCrawlerMessage;

            _cancellationTokenSource?.Cancel();
            _initializationSemaphore?.Dispose();
            //((IDisposable)_puppeteerEngine)?.Dispose();
            //_kernel?.Dispose();
        }
    }
}
