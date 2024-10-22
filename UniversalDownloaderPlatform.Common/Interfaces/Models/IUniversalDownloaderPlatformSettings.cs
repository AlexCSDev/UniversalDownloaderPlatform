using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UniversalDownloaderPlatform.Common.Enums;

namespace UniversalDownloaderPlatform.Common.Interfaces.Models
{
    /// <summary>
    /// Interface implementing basic settings UniversalDownloader expects.
    /// No changes should be made to values once this has been passed to UniversalDownloader!
    /// </summary>
    public interface IUniversalDownloaderPlatformSettings
    {
        /// <summary>
        /// Cookie container with all required cookies, can be null
        /// </summary>
        CookieContainer CookieContainer { get; set; }

        /// <summary>
        /// User agent string, if set to null will be retrieved from ICookieRetriever
        /// </summary>
        string UserAgent { get; set; }

        /// <summary>
        /// Urls containing anything from this list will be ignored by all components
        /// </summary>
        List<string> UrlBlackList { get; init; }

        /// <summary>
        /// The amount of times to retry download on failure
        /// </summary>
        int MaxDownloadRetries { get; init; }

        /// <summary>
        /// Multiplier which will be used to calculate time between retries
        /// </summary>
        int RetryMultiplier { get; init; }

        /// <summary>
        /// What downloader components should do when the file already exists on disk
        /// (might be ignored by plugins and non-default web downloader class)
        /// </summary>
        FileExistsAction FileExistsAction { get; init; }

        /// <summary>
        /// Check remote file size for already existing files if available
        /// (might be ignored by plugins and non-default web downloader class)
        /// </summary>
        bool IsCheckRemoteFileSize { get; init; }

        string ProxyServerAddress { get; init; }

        /// <summary>
        /// The base download directory for the downloaded files. If not set will be set to appdir\download\ICrawlTargetInfo.SaveDirectory. IMPORTANT: THIS VALUE CAN AND PROBABLY WILL CHANGE AFTER BeforeStart METHODS ARE CALLED. DO NOT COPY IT IN THIS METHOD UNLESS EXPLICITLY ALLOWED.
        /// </summary>
        string DownloadDirectory { get; set; } //seems like you can't clone records implementing interface using *with* keyword, so can't use init here unfortunately.
    }
}
