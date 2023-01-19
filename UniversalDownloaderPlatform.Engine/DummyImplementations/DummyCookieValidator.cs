using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    internal class DummyCookieValidator : ICookieValidator
    {
        public Task ValidateCookies(CookieContainer cookieContainer)
        {
            return Task.CompletedTask;
        }
    }
}
