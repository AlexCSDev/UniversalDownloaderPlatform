using System;
using System.Net;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    /// <summary>
    /// Interface for additional implementations of cookie retrievers. WARNING: ICookieRetriever is called BEFORE anything else. None of the UDP components are initialized at this point and you should attempt not reference them.
    /// </summary>
    public interface ICookieRetriever : IDisposable
    {
        /// <summary>
        /// Initialization function, called on every UniversalDownloader.Download call
        /// </summary>
        /// <returns></returns>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);

        Task<string> GetUserAgent();
        Task<CookieContainer> RetrieveCookies();
    }
}
