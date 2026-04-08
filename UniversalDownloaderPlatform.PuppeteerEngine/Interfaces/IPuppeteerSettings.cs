using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalDownloaderPlatform.PuppeteerEngine.Interfaces
{
    public interface IPuppeteerSettings
    {
        /// <summary>
        /// Address of the page used to login
        /// </summary>
        public string LoginPageAddress { get; }
        /// <summary>
        /// Address of the page used to check if we are currently logged in
        /// </summary>
        public string LoginCheckAddress { get; }
        /// <summary>
        /// Address of the page used to retrieve cookies. If set to null then url of the page where captcha is triggered will be used.
        /// </summary>
        public string CaptchaCookieRetrievalAddress { get; }
        /// <summary>
        /// Address of API request for checking account existence used by Patreon login page. Finishing this request signifies being ready to enter password.
        /// </summary>
        public string AuthAddress { get; }
        /// <summary>
        /// Address of the remote browser, if not set internal browser will be used
        /// </summary>
        public Uri RemoteBrowserAddress { get; init; }
        /// <summary>
        /// (Internal browser only) Should browser window be visible or not
        /// </summary>
        public bool IsHeadlessBrowser { get; init; }
        /// <summary>
        /// Proxy server address
        /// </summary>
        public string ProxyServerAddress { get; init; }
        /// <summary>
        /// Email used for optional automatic login
        /// </summary>
        public string LoginEmail { get; init; }
        /// <summary>
        /// Password used for optional automatic login
        /// </summary>
        public string LoginPassword { get; init; }
    }
}
