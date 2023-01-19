using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface ICaptchaSolver
    {
        /// <summary>
        /// Initialization function, called on every UniversalDownloader.Download call
        /// </summary>
        /// <returns></returns>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);

        Task<bool> IsCaptchaTriggered(HttpResponseMessage responseMessage);
        /// <summary>
        /// Solve captcha
        /// </summary>
        /// <param name="url">Url of the page behind cookie check</param>
        /// <returns></returns>
        Task<CookieCollection> SolveCaptcha(string url);
    }
}
