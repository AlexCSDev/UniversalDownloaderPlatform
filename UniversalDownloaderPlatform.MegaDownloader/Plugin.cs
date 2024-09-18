using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using NLog;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using DownloadException = UniversalDownloaderPlatform.Common.Exceptions.DownloadException;

namespace UniversalDownloaderPlatform.MegaDownloader
{
    public class Plugin : IPlugin
    {
        public string Name => "Mega.nz Downloader";
        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/AlexCSDev/UniversalDownloaderPlatform";

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private bool _overwriteFiles;

        private readonly static Regex _newFormatRegex;
        private readonly static Regex _oldFormatRegex;
        private readonly static MegaCredentials _megaCredentials;
        private static MegaDownloader _megaDownloader;

        private IUniversalDownloaderPlatformSettings _settings;

        static Plugin()
        {
            //todo: replace with MegaUrlDataExtractor
            _newFormatRegex = new Regex(@"/(?<type>(file|folder))/(?<id>[^#/ ]+)(#(?<key>[a-zA-Z0-9_-]+))?");//Regex("(#F|#)![a-zA-Z0-9]{0,8}![a-zA-Z0-9_-]+");
            _oldFormatRegex = new Regex("#(?<type>F?)!(?<id>[^!]+)(!(?<key>[^$!\\?<'\"\\s]+))?");

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mega_credentials.json");

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(configPath, true, false)
                .Build();

            if (!File.Exists(configPath))
            {
                LogManager.GetCurrentClassLogger().Warn("!!!![MEGA]: mega_credentials.json not found, mega downloading will be limited! Refer to documentation for additional information. !!!!");
            }
            else
            {
                _megaCredentials = new MegaCredentials(configuration["email"], configuration["password"]);
            }

            try
            {
                _megaDownloader = new MegaDownloader(_megaCredentials);
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Fatal("!!!![MEGA]: Unable to initialize mega downloader, check email and password! No mega files will be downloaded in this session. !!!!");
            }
        }

        public void OnLoad(IDependencyResolver dependencyResolver)
        {
            //do nothing
        }

        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _settings = settings;
			
            if (_megaDownloader != null)
				_megaDownloader.BeforeStart(settings.MaxDownloadRetries, settings.IsCheckRemoteFileSize);
/*### How can I use this library when I have a proxy with authentication?
You can add the following line to specify proxy credentials for the whole application.
```csharp
WebRequest.DefaultWebProxy.Credentials = New NetworkCredentails("Username", "Password")
```*/
            return Task.CompletedTask;
        }

        public async Task Download(ICrawledUrl crawledUrl)
        {
            if (_megaDownloader == null)
            {
                _logger.Fatal($"Mega downloader initialization failure (check credentials), {crawledUrl.Url} will not be downloaded!");
                return;
            }

            try
            {
                await _megaDownloader.DownloadUrlAsync(crawledUrl, _settings.DownloadDirectory, _settings.FileExistsAction);
            }
            catch (DownloadException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Common.Exceptions.DownloadException($"Unable to download {crawledUrl.Url}: {ex}", ex);
            }
        }

        public Task<List<string>> ExtractSupportedUrls(string htmlContents)
        {
            List<string> retList = new List<string>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContents);

            string parseText = string.Join(" ", doc.DocumentNode.Descendants()
                .Where(n => !n.HasChildNodes && !string.IsNullOrWhiteSpace(n.InnerText))
                .Select(n => n.InnerText)); //first get a copy of text without all html tags
            parseText += doc.DocumentNode.InnerHtml; //now append a copy of this text with all html tags intact (otherwise we lose all <a href=... links)
            parseText = parseText // TODO: replace with with something better
                .Replace("</a> ", "")
                .Replace("</a>", "")
                .Replace("<a>", "")
                .Replace("<a", ""); //trying to get rid of the situations where key is outside of anchor tag.

            MatchCollection matchesNewFormat = _newFormatRegex.Matches(parseText);

            MatchCollection matchesOldFormat = _oldFormatRegex.Matches(parseText);

            _logger.Debug($"Found NEW:{matchesNewFormat.Count}|OLD:{matchesOldFormat.Count} possible mega links in description");

            List<string> megaUrls = new List<string>();

            foreach (Match match in matchesNewFormat)
            {
                _logger.Debug($"Parsing mega match new format {match.Value}");
                if (!match.Groups["key"].Success || string.IsNullOrWhiteSpace(match.Groups["key"].Value))
                {
                    _logger.Warn("Mega url without key, will be skipped");
                    continue;
                }
                megaUrls.Add($"https://mega.nz/{match.Groups["type"].Value.Trim()}/{match.Groups["id"].Value.Trim()}#{match.Groups["key"].Value.Trim()}");
            }

            foreach (Match match in matchesOldFormat)
            {
                _logger.Debug($"Parsing mega match old format {match.Value}");
                if (!match.Groups["key"].Success || string.IsNullOrWhiteSpace(match.Groups["key"].Value))
                {
                    _logger.Warn("Mega url without key, will be skipped");
                    continue;
                }

                string type = match.Groups["type"].Value.Trim() == "F" ? "folder" : "file";
                megaUrls.Add($"https://mega.nz/{type}/{match.Groups["id"].Value.Trim()}#{match.Groups["key"].Value.Trim()}");
            }

            foreach (string url in megaUrls)
            {
                string sanitizedUrl = url.Split(' ')[0].Replace("&lt;wbr&gt;", "").Replace("&lt;/wbr&gt;", "");
                _logger.Debug($"Adding mega match {sanitizedUrl}");
                if (retList.Contains(sanitizedUrl))
                {
                    _logger.Debug($"Already parsed, skipping: {sanitizedUrl}");
                    continue;
                }
                retList.Add(sanitizedUrl);
            }

            return Task.FromResult(retList);
        }

        public Task<bool> IsSupportedUrl(string url)
        {
            if (!url.Contains("mega.nz/") && !url.Contains("mega.co.nz/"))
                return Task.FromResult(false);

            MatchCollection matchesNewFormat = _newFormatRegex.Matches(url);
            MatchCollection matchesOldFormat = _oldFormatRegex.Matches(url);

            if (matchesOldFormat.Count > 0 || matchesNewFormat.Count > 0)
                return Task.FromResult(true);

            return Task.FromResult(false);
        }

        public Task<bool> ProcessCrawledUrl(ICrawledUrl crawledUrl)
        {
            if(crawledUrl.Url.StartsWith("https://mega.nz/"))
            {
                _logger.Debug($"Mega found: {crawledUrl.Url}");
                return Task.FromResult(true); //mega plugin expects to see only path to the folder where everything will be saved
            }

            return Task.FromResult(false);
        }
    }
}
