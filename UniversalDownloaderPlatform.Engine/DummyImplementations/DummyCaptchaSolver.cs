using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    internal class DummyCaptchaSolver : ICaptchaSolver
    {
        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            return Task.CompletedTask;
        }

        public Task<bool> IsCaptchaTriggered(HttpResponseMessage responseMessage)
        {
            return Task.FromResult(false);
        }

        public Task<CookieCollection> SolveCaptcha(string url)
        {
            return Task.FromResult(new CookieCollection());
        }
    }
}
