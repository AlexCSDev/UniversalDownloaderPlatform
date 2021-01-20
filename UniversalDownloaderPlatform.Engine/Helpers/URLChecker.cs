using System;
using System.Linq;
using NLog;

namespace UniversalDownloaderPlatform.Engine.Helpers
{
    internal static class UrlChecker
    {
        private static string[] _blackList = (ConfigurationManager.Configuration["UrlBlackList"] ?? "").ToLowerInvariant().Split("|");

        /// <summary>
        /// Checks that url is a valid url and is not blacklisted
        /// </summary>
        /// <param name="url">Url to check</param>
        /// <returns></returns>
        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            Uri uriResult;
            bool validationResult = Uri.TryCreate(url, UriKind.Absolute, out uriResult) &&
                                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            return validationResult;
        }

        /// <summary>
        /// Checks that url is a valid url is not blacklisted
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsBlacklistedUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            string lowerUrl = url.ToLowerInvariant();
            return _blackList.Any(x => lowerUrl.Contains(x));
        }
    }
}
