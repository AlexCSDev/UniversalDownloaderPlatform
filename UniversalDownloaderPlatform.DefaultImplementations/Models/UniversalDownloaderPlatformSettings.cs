using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UniversalDownloaderPlatform.Common.Helpers;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.DefaultImplementations.Models
{
    public class UniversalDownloaderPlatformSettings : IUniversalDownloaderPlatformSettings
    {
        private const string DefaultUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Safari/537.36 Edge/13.10586";
        private const int DefaultMaxDownloadRetries = 10;
        private const int DefaultRetryMultiplier = 5;

        private bool _overwriteFiles;
        private CookieContainer _cookieContainer;
        private string _userAgent;
        private List<string> _urlBlackList;
        private int _maxDownloadRetries;
        private int _retryMultiplier;
        private bool _consumed;

        public bool OverwriteFiles
        {
            get => _overwriteFiles;
            set => ConsumableSetter.Set(Consumed, ref _overwriteFiles, value);
        }

        public CookieContainer CookieContainer
        {
            get => _cookieContainer;
            set => ConsumableSetter.Set(Consumed, ref _cookieContainer, value);
        }

        public string UserAgent
        {
            get => _userAgent;
            set
            {
                string newValue = value;
                if (newValue == null)
                    newValue = DefaultUserAgent;

                ConsumableSetter.Set(Consumed, ref _userAgent, newValue);
            }
        }

        public List<string> UrlBlackList
        {
            get => _urlBlackList;
            set
            {
                ConsumableSetter.Set(Consumed, ref _urlBlackList, value);
            }
        }

        public int MaxDownloadRetries
        {
            get => _maxDownloadRetries;
            set
            {
                ConsumableSetter.Set(Consumed, ref _maxDownloadRetries, value);
            }
        }

        public int RetryMultiplier
        {
            get => _retryMultiplier;
            set
            {
                ConsumableSetter.Set(Consumed, ref _retryMultiplier, value);
            }
        }

        public bool Consumed
        {
            get => _consumed;
            set => ConsumableSetter.Set(Consumed, ref _consumed, value);
        }

        public UniversalDownloaderPlatformSettings()
        {
            _userAgent = DefaultUserAgent;
            _maxDownloadRetries = DefaultMaxDownloadRetries;
            _retryMultiplier = DefaultRetryMultiplier;
        }
    }
}
