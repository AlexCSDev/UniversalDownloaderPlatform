using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

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
        /// Any attempt to set properties will result in exception if this set to true. Refer to documentation for details on proper implementation.
        /// </summary>
        bool Consumed { get; set; }
    }
}
