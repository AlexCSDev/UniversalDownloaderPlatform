using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UniversalDownloaderPlatform.Common.Enums;

namespace UniversalDownloaderPlatform.Common.Interfaces.Models
{
    public interface IUniversalDownloaderPlatformSettings
    {
        /// <summary>
        /// If true IWebDownloader will be allowed to overwrite files
        /// </summary>
        bool OverwriteFiles { get; set; }

        /// <summary>
        /// Cookie container with all required cookies, can be null
        /// </summary>
        CookieContainer CookieContainer { get; set; }

        /// <summary>
        /// User agent string
        /// </summary>
        string UserAgent { get; set; }

        /// <summary>
        /// Urls containing anything from this list will be ignored by all components
        /// </summary>
        List<string> UrlBlackList { get; set; }

        /// <summary>
        /// The amount of times to retry download on failure
        /// </summary>
        int MaxDownloadRetries { get; set; }

        /// <summary>
        /// Multiplier which will be used to calculate time between retries
        /// </summary>
        int RetryMultiplier { get; set; }

        /// <summary>
        /// What downloader components should do when the remote file's size not available (Content-Length is 0 or empty for http(s) requests) and cannot be used to check existing file for validity
        /// (might be ignored by plugins and non-default web downloader class)
        /// </summary>
        RemoteFileSizeNotAvailableAction RemoteFileSizeNotAvailableAction { get; set; }

        string ProxyServerAddress { get; set; }

        /// <summary>
        /// Any attempt to set properties will result in exception if this set to true. Refer to documentation for details on proper implementation.
        /// </summary>
        bool Consumed { get; set; }
    }
}
