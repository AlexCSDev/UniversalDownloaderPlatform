using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    internal class DummyCookieRetriever : ICookieRetriever
    {
        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetUserAgent()
        {
            return Task.FromResult("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Safari/537.36 Edge/13.10586");
        }

        public Task<CookieContainer> RetrieveCookies()
        {
            return Task.FromResult(new CookieContainer());
        }
    }
}
