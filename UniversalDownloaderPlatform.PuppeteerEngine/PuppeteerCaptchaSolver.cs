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
using System.Net.Http.Headers;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using System.Runtime;
using System.Threading;
using System.Net.Http;

namespace UniversalDownloaderPlatform.PuppeteerEngine
{
    public class PuppeteerCaptchaSolver : ICaptchaSolver
    {
        private readonly string _proxyServerAddress;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private IPuppeteerEngine _puppeteerEngine;
        private IPuppeteerSettings _settings;

        private SemaphoreSlim _captchaSolvingSemaphore;

        /// <summary>
        /// Create new instance of PuppeteerCaptchaSolver using internal browser
        /// </summary>
        public PuppeteerCaptchaSolver()
        {
            _captchaSolvingSemaphore = new SemaphoreSlim(1, 1);
        }

        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _settings = settings as IPuppeteerSettings;

            return Task.CompletedTask;
        }

        private async Task<IWebBrowser> RestartBrowser(bool headless)
        {
            await _puppeteerEngine.CloseBrowser();
            await Task.Delay(1000); //safety first

            _puppeteerEngine = new PuppeteerEngine(headless, _proxyServerAddress);
            return await _puppeteerEngine.GetBrowser();
        }
        
        public virtual async Task<CookieCollection> SolveCaptcha(string url)
        {
            if(_puppeteerEngine == null)
                _puppeteerEngine = new PuppeteerEngine(true, _settings.ProxyServerAddress);

            try
            {
                await _captchaSolvingSemaphore.WaitAsync();

                _logger.Debug("Calling captcha check");
                try
                {
                    await CaptchaCheck(url);
                }
                catch (Exception ex)
                {
                    _logger.Fatal($"Captcha solver error: {ex.Message}");
                    return null;
                }

                _logger.Debug("Retrieving browser");
                IWebBrowser browser = await _puppeteerEngine.GetBrowser();

                _logger.Debug("Retrieving cookies");
                IWebPage page = await browser.NewPageAsync();
                await page.GoToAsync(_settings.CaptchaCookieRetrievalAddress ?? url);

                CookieParam[] browserCookies = await page.GetCookiesAsync();

                CookieCollection cookieCollection = new CookieCollection();

                if (browserCookies != null && browserCookies.Length > 0)
                {
                    foreach (CookieParam browserCookie in browserCookies)
                    {
                        _logger.Debug($"Adding cookie: {browserCookie.Name}");
                        Cookie cookie = new Cookie(browserCookie.Name, browserCookie.Value, browserCookie.Path, browserCookie.Domain);
                        cookieCollection.Add(cookie);
                    }
                }
                else
                {
                    _logger.Fatal("No cookies were extracted from browser");
                    return null;
                }

                await page.CloseAsync();

                return cookieCollection;
            }
            catch (TimeoutException ex)
            {
                _logger.Fatal($"Internal operation timed out. Exception: {ex}");
                return null;
            }
            finally
            {

                if (_puppeteerEngine != null)
                {
                    _puppeteerEngine.Dispose();
                    _puppeteerEngine = null;
                }

                _captchaSolvingSemaphore.Release();
            }
        }

        protected virtual async Task CaptchaCheck(string url)
        {
            _logger.Debug("Retrieving browser");
            IWebBrowser browser = await _puppeteerEngine.GetBrowser();

            IWebPage page = null;
            bool passedCaptcha = false;
            do
            {
                if (page == null || page.IsClosed)
                    page = await browser.NewPageAsync();

                _logger.Debug("Checking if captcha is returned");
                IWebResponse response = await page.GoToAsync(url);
                if (response.Status == HttpStatusCode.Forbidden)
                {
                    _logger.Debug("Got 403 forbidden (possibly captcha), opening page");
                    if (_puppeteerEngine.IsHeadless)
                    {
                        _logger.Debug("Puppeteer is in headless mode, restarting in full mode");
                        browser = await RestartBrowser(false);
                        page = await browser.NewPageAsync();
                    }

                    await page.GoToAsync(url);

                    await page.WaitForRequestAsync(request => request.Url == url);
                }
                else
                {
                    _logger.Debug("Captcha was not returned, done");

                    passedCaptcha = true;
                }
            } while (!passedCaptcha);

            await page.CloseAsync();
        }

        public async Task<bool> IsCaptchaTriggered(HttpResponseMessage responseMessage)
        {
            //typical cloudflare captcha check
            if (responseMessage.StatusCode == HttpStatusCode.OK)
                return false;

            string content = await responseMessage.Content.ReadAsStringAsync();
            return responseMessage.StatusCode == HttpStatusCode.Forbidden && 
                content.ToLowerInvariant().Contains("https://ct.captcha-delivery.com/c.js");
        }
    }
}
