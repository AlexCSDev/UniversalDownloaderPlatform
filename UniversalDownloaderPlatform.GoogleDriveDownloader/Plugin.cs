using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;

namespace UniversalDownloaderPlatform.GoogleDriveDownloader
{
    public sealed class Plugin : IPlugin
    {
        public string Name => "Google Drive Downloader";
        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/AlexCSDev/UniversalDownloaderPlatform";

        private static readonly Regex _googleDriveRegex;
        private static readonly Regex _googleDocsRegex;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly GoogleDriveEngine _engine;

        private IUniversalDownloaderPlatformSettings _settings;

        static Plugin()
        {
            if (!System.IO.File.Exists("gd_credentials.json"))
            {
                LogManager.GetCurrentClassLogger().Fatal("!!!![GOOGLE DRIVE]: gd_credentials.json not found, google drive files will not be downloaded! Refer to documentation for additional information. !!!!");
            }

            _googleDriveRegex = new Regex("https:\\/\\/drive\\.google\\.com\\/(?:file\\/d\\/|open\\?id\\=|drive\\/folders\\/|folderview\\?id=|drive\\/u\\/[0-9]+\\/folders\\/)([A-Za-z0-9_-]+)");
            _googleDocsRegex = new Regex("https:\\/\\/docs\\.google\\.com\\/(?>document|spreadsheets)\\/d(?>\\/e)?\\/([a-zA-Z0-9-_]+)");
            _engine = new GoogleDriveEngine();
        }

        public void OnLoad(IDependencyResolver dependencyResolver)
        {
            //do nothing
        }

        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _settings = settings;

            return Task.CompletedTask;
        }

        public Task<bool> IsSupportedUrl(string url)
        {
            Match driveMatch = _googleDriveRegex.Match(url);
            Match docsMatch = _googleDocsRegex.Match(url);

            return Task.FromResult(driveMatch.Success || docsMatch.Success);
        }

        public Task Download(ICrawledUrl crawledUrl)
        {
            _logger.Debug($"Received new url: {crawledUrl.Url}");

            Match match = _googleDriveRegex.Match(crawledUrl.Url);
            if (!match.Success)
            {
                match = _googleDocsRegex.Match(crawledUrl.Url);
                if(!match.Success)
                {
                    _logger.Error($"Unable to parse google drive/docs url: {crawledUrl.Url}");
                    throw new DownloadException($"Unable to parse google drive/docs url: {crawledUrl.Url}");
                }
            }

            string id = match.Groups[1].Value;

            string downloadPath = Path.Combine(_settings.DownloadDirectory, crawledUrl.DownloadPath);
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
                _engine.Download(id, downloadPath, _settings.FileExistsAction, _settings.IsCheckRemoteFileSize);
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

        public Task<bool> ProcessCrawledUrl(ICrawledUrl crawledUrl)
        {
            if (_googleDriveRegex.Match(crawledUrl.Url).Success || _googleDocsRegex.Match(crawledUrl.Url).Success)
            {
                _logger.Debug($"Google drive/docs found: {crawledUrl.Url}");
                return Task.FromResult(true); //skip all checks
            }

            return Task.FromResult(false);
        }
    }
}
