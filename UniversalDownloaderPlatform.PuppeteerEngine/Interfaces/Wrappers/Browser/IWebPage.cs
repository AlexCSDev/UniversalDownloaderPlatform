using System;
using System.Threading.Tasks;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace UniversalDownloaderPlatform.PuppeteerEngine.Interfaces.Wrappers.Browser
{
    /// <summary>
    /// This interface is a wrapper around a Puppeteer Sharp's page object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    public interface IWebPage
    {
        bool IsClosed { get; }
        Task<IWebResponse> GoToAsync(string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null);
        Task SetUserAgentAsync(string userAgent);
        Task<string> GetContentAsync();
        Task<IWebRequest> WaitForRequestAsync(Func<IRequest, bool> predicate, WaitForOptions options = null);
        Task<IWebResponse> WaitForResponseAsync(Func<IResponse, bool> predicate, WaitForOptions options = null);
        Task WaitForNetworkIdleAsync(WaitForNetworkIdleOptions options = null);
        Task WaitForSelectorAsync(string selector, WaitForSelectorOptions options = null);
        Task TypeAsync(string selector, string text, TypeOptions options = null);
        Task ClickAsync(string selector, ClickOptions options = null);
        Task<CookieParam[]> GetCookiesAsync(params string[] urls);
        Task CloseAsync(PageCloseOptions options = null);
    }
}
