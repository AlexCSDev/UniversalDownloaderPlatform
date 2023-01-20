using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Helpers;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.DefaultImplementations.Models
{
    public record UniversalDownloaderPlatformSettings : IUniversalDownloaderPlatformSettings
    {
        private const int DefaultMaxDownloadRetries = 10;
        private const int DefaultRetryMultiplier = 5;
        private const FileExistsAction DefaultRemoteFileSizeNotAvailableAction =
            FileExistsAction.BackupIfDifferent;
        private const bool DefaultIsCheckRemoteFileSize = true;

        private int _maxDownloadRetries;
        private int _retryMultiplier;

        public CookieContainer CookieContainer { get; set; }

        public string UserAgent { get; set; }

        public List<string> UrlBlackList { get; init; }

        public int MaxDownloadRetries
        {
            get => _maxDownloadRetries;
            init
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
                _maxDownloadRetries = value;
            }
        }

        public int RetryMultiplier
        {
            get => _retryMultiplier;
            init
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
                _retryMultiplier = value;
            }
        }

        public FileExistsAction FileExistsAction { get; init; }

        public bool IsCheckRemoteFileSize { get; init; }

        public string ProxyServerAddress { get; init; }

        public string DownloadDirectory { get; set; }

        public UniversalDownloaderPlatformSettings()
        {
            _maxDownloadRetries = DefaultMaxDownloadRetries;
            _retryMultiplier = DefaultRetryMultiplier;
            FileExistsAction = DefaultRemoteFileSizeNotAvailableAction;
            IsCheckRemoteFileSize = DefaultIsCheckRemoteFileSize;
            DownloadDirectory = null;
        }
    }
}
