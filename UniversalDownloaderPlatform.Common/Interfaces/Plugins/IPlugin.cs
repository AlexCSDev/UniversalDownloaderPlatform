using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Author { get; }
        string ContactInformation { get; }

        /// <summary>
        /// Called when plugin is loaded
        /// </summary>
        /// <returns></returns>
        /// <param name="dependencyResolver">Dependency resolver</param>
        /// <returns></returns>
        void OnLoad(IDependencyResolver dependencyResolver);

        /// <summary>
        /// Initialization function, called by IPluginManager's BeforeStart() function.
        /// </summary>
        /// <returns></returns>
        /// <param name="settings">Settings for the current download task</param>
        /// <returns></returns>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);
        /// <summary>
        /// Returns true if supplied url is supported by this plugin
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task<bool> IsSupportedUrl(string url);
        /// <summary>
        /// Download crawled url
        /// </summary>
        /// <param name="crawledUrl"></param>
        /// <returns></returns>
        Task Download(ICrawledUrl crawledUrl);

        /// <summary>
        /// Extract supported urls from supplied html text
        /// </summary>
        /// <param name="htmlContents"></param>
        /// <returns></returns>
        Task<List<string>> ExtractSupportedUrls(string htmlContents);

        /// <summary>
        /// Called before passing crawledUrl to ICrawledUrlProcessor, return true if you would like minimal processing by ICrawledUrlProcessor otherwise return false
        /// </summary>
        /// <param name="crawledUrl"></param>
        /// <returns></returns>
        Task<bool> ProcessCrawledUrl(ICrawledUrl crawledUrl);
    }
}
