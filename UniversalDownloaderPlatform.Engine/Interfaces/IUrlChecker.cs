using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Engine.Interfaces
{
    internal interface IUrlChecker
    {
        /// <summary>
        /// Initialization function, called on every UniversalDownloader.Download call
        /// </summary>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);
        /// <summary>
        /// Checks that url is a valid url and is not blacklisted
        /// </summary>
        /// <param name="url">Url to check</param>
        /// <returns></returns>
        bool IsValidUrl(string url);
        /// <summary>
        /// Checks that url is a valid url is not blacklisted
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        bool IsBlacklistedUrl(string url);
    }
}
