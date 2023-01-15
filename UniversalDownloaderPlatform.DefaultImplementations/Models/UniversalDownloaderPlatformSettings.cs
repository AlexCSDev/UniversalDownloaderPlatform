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
        private const string DefaultUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Safari/537.36 Edge/13.10586";
        private const int DefaultMaxDownloadRetries = 10;
        private const int DefaultRetryMultiplier = 5;
        private const FileExistsAction DefaultRemoteFileSizeNotAvailableAction =
            FileExistsAction.BackupIfDifferent;

        private string _userAgent;
        private int _maxDownloadRetries;
        private int _retryMultiplier;

        public CookieContainer CookieContainer { get; init; }

        public string UserAgent
        {
            get => _userAgent;
            init
            {
                string newValue = value;
                if (newValue == null)
                    newValue = DefaultUserAgent;

                _userAgent = newValue;
            }
        }

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
            _userAgent = DefaultUserAgent;
            _maxDownloadRetries = DefaultMaxDownloadRetries;
            _retryMultiplier = DefaultRetryMultiplier;
            FileExistsAction = DefaultRemoteFileSizeNotAvailableAction;
            DownloadDirectory = null;
        }
    }
}
