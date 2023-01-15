using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;

namespace UniversalDownloaderPlatform.GoogleDriveDownloader
{
    public sealed class Plugin : IPlugin
    {
        public string Name => "Google Drive Downloader";
        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/Megalan/PatreonDownloader";

        private static readonly Regex _googleDriveRegex;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly GoogleDriveEngine _engine;

        private FileExistsAction _fileExistsAction;
        private string _downloadDirectory;

        static Plugin()
        {
            if (!System.IO.File.Exists("gd_credentials.json"))
            {
                LogManager.GetCurrentClassLogger().Fatal("!!!![GOOGLE DRIVE]: gd_credentials.json not found, google drive files will not be downloaded! Refer to documentation for additional information. !!!!");
            }

            _googleDriveRegex = new Regex("https:\\/\\/drive\\.google\\.com\\/(?:file\\/d\\/|open\\?id\\=|drive\\/folders\\/|folderview\\?id=|drive\\/u\\/[0-9]+\\/folders\\/)([A-Za-z0-9_-]+)");
            _engine = new GoogleDriveEngine();
        }

        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _fileExistsAction = settings.FileExistsAction;
            _downloadDirectory = settings.DownloadDirectory;

            return Task.CompletedTask;
        }

        public Task<bool> IsSupportedUrl(string url)
        {
            Match match = _googleDriveRegex.Match(url);

            return Task.FromResult(match.Success);
        }

        public Task Download(ICrawledUrl crawledUrl)
        {
            _logger.Debug($"Received new url: {crawledUrl.Url}");

            Match match = _googleDriveRegex.Match(crawledUrl.Url);
            if (!match.Success)
            {
                _logger.Error($"Unable to parse google drive url: {crawledUrl.Url}");
                throw new DownloadException($"Unable to parse google drive url: {crawledUrl.Url}");
            }

            string id = match.Groups[1].Value;

            string downloadPath = Path.Combine(_downloadDirectory, crawledUrl.DownloadPath);
            try
            {
                //warning: returns '' in drive's root
                if (!Directory.Exists(downloadPath))
                    Directory.CreateDirectory(downloadPath);
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to create directory for file {crawledUrl.Url}", ex);
            }

            downloadPath = Path.Combine(downloadPath, $"{id.Substring(id.Length - 6, 5)}_")
                .TrimEnd(new[] { '/', '\\' });

            _logger.Debug($"Retrieved id: {id}, download path: {downloadPath}");

            try
            {
                _engine.Download(id, downloadPath, _fileExistsAction);
            }
            catch (Exception ex)
            {
                _logger.Error("GOOGLE DRIVE ERROR: " + ex);
                throw new DownloadException($"Unable to download {crawledUrl.Url}", ex);
            }

            return Task.CompletedTask;
        }

        public Task<List<string>> ExtractSupportedUrls(string htmlContents)
        {
            //Let default plugin do this
            return Task.FromResult((List<string>)null);
        }
    }
}
