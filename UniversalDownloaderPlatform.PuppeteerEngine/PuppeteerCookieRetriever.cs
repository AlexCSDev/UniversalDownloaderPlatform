using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Interfaces;
using PuppeteerSharp;
using UniversalDownloaderPlatform.PuppeteerEngine.Interfaces;
using UniversalDownloaderPlatform.PuppeteerEngine.Interfaces.Wrappers.Browser;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.PuppeteerEngine
{
    /// <summary>
    /// Somewhat universal cookie retriever based on chromium browser
    /// </summary>
    public class PuppeteerCookieRetriever : ICookieRetriever, IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private IPuppeteerEngine _puppeteerEngine;
        private IPuppeteerSettings _settings;
        private bool _isHeadlessBrowser;
        private bool _isRemoteBrowser;


        /// <summary>
        /// Create new instance of PuppeteerCookieRetriever
        /// </summary>
        /// <param name="loginPage">Address which will be used to open login page</param>
        /// <param name="loginCheckPage">Address which will be used to check if user is logged in</param>
        /// <param name="remoteBrowserAddress">Address of the remote chromium instance. If set to null then internal copy will be used.</param>
        /// <param name="headlessBrowser">If set to false then the internal browser will be visible, ignored if remote browser is used</param>
        /// <param name="proxyServerAddress">Address of the proxy server to use (null for no proxy server)</param>
        public PuppeteerCookieRetriever()
        {
        }

        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _settings = settings as IPuppeteerSettings;

            if (_settings.RemoteBrowserAddress != null)
            {
                _puppeteerEngine = new PuppeteerEngine(_settings.RemoteBrowserAddress) { UserAgent = settings.UserAgent };
                _isHeadlessBrowser = true;
                _isRemoteBrowser = true;
            }
            else
            {
                _isHeadlessBrowser = false;
                _isRemoteBrowser = false;
                _puppeteerEngine = new PuppeteerEngine(_isHeadlessBrowser, _settings.ProxyServerAddress) { UserAgent = settings.UserAgent };
            }

            return Task.CompletedTask;
        }

        private async Task<IWebBrowser> RestartBrowser(bool headless)
        {
            await _puppeteerEngine.CloseBrowser();
            await Task.Delay(1000); //safety first

            _puppeteerEngine = new PuppeteerEngine(headless, _settings.ProxyServerAddress);
            return await _puppeteerEngine.GetBrowser();
        }

        protected virtual async Task Login()
        {
            _logger.Debug("Retrieving browser");
            IWebBrowser browser = await _puppeteerEngine.GetBrowser();

            IWebPage page = null;
            bool loggedIn = false;
            do
            {
                if (page == null || page.IsClosed)
                    page = await browser.NewPageAsync();

                _logger.Debug("Checking login status");
                IWebResponse response = await page.GoToAsync(_settings.LoginCheckAddress);
                if (!await IsLoggedIn(response))
                {
                    _logger.Debug("We are NOT logged in, opening login page");
                    if (_isRemoteBrowser)
                    {
                        await page.CloseAsync();
                        throw new Exception("You are not logged in into your account in remote browser. Please login and restart application.");
                    }
                    if (_puppeteerEngine.IsHeadless)
                    {
                        _logger.Debug("Puppeteer is in headless mode, restarting in full mode");
                        browser = await RestartBrowser(false);
                        page = await browser.NewPageAsync();
                    }

                    await page.GoToAsync(_settings.LoginPageAddress);

                    await page.WaitForRequestAsync(request => request.Url.Contains(_settings.LoginCheckAddress));
                }
                else
                {
                    _logger.Debug("We are logged in");
                    if (_puppeteerEngine.IsHeadless != _isHeadlessBrowser)
                    {
                        browser = await RestartBrowser(_isHeadlessBrowser);
                        page = await browser.NewPageAsync();
                    }

                    loggedIn = true;
                }
            } while (!loggedIn);

            await page.CloseAsync();
        }

        /// <summary>
        /// Perform check if the received response contains data which can be used to assume that we are logged in
        /// </summary>
        /// <param name="response"></param>
        /// <returns>True if logged in, false if not logged in</returns>
        protected virtual Task<bool> IsLoggedIn(IWebResponse response)
        {
            return Task.FromResult(response.Status != HttpStatusCode.Unauthorized && response.Status != HttpStatusCode.Forbidden);
        }

        public virtual async Task<CookieContainer> RetrieveCookies()
        {
            try
            {
                CookieContainer cookieContainer = new CookieContainer(1000, 100, CookieContainer.DefaultCookieLengthLimit);

                _logger.Debug("Calling login check");
                try
                {
                    await Login();
                }
                catch (Exception ex)
                {
                    _logger.Fatal($"Login error: {ex.Message}");
                    return null;
                }

                _logger.Debug("Retrieving browser");
                IWebBrowser browser = await _puppeteerEngine.GetBrowser();

                _logger.Debug("Retrieving cookies");
                IWebPage page = await browser.NewPageAsync();
                await page.GoToAsync(_settings.LoginCheckAddress);

                CookieParam[] browserCookies = await page.GetCookiesAsync();

                if (browserCookies != null && browserCookies.Length > 0)
                {
                    foreach (CookieParam browserCookie in browserCookies)
                    {
                        _logger.Debug($"Adding cookie: {browserCookie.Name}");
                        Cookie cookie = new Cookie(browserCookie.Name, browserCookie.Value, browserCookie.Path, browserCookie.Domain);
                        cookieContainer.Add(cookie);
                    }
                }
                else
                {
                    _logger.Fatal("No cookies were extracted from browser");
                    return null;
                }

                await page.CloseAsync();

                return cookieContainer;
            }
            catch (TimeoutException ex)
            {
                _logger.Fatal($"Internal operation timed out. Exception: {ex}");
                return null;
            }
        }

        public async Task<string> GetUserAgent()
        {
            IWebBrowser browser = await _puppeteerEngine.GetBrowser();
            return await browser.GetUserAgentAsync();
        }

        public void Dispose()
        {
            _puppeteerEngine?.Dispose();
        }
    }
}
