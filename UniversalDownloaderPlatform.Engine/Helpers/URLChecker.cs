using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Engine.Interfaces;

namespace UniversalDownloaderPlatform.Engine.Helpers
{
    internal class UrlChecker : IUrlChecker
    {
        private List<string> _blackList;

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _blackList = settings.UrlBlackList;
        }

        public bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            Uri uriResult;
            bool validationResult = Uri.TryCreate(url, UriKind.Absolute, out uriResult) &&
                                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            return validationResult;
        }

        public bool IsBlacklistedUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            string lowerUrl = url.ToLowerInvariant();
            return _blackList.Any(x => lowerUrl.Contains(x));
        }
    }
}
