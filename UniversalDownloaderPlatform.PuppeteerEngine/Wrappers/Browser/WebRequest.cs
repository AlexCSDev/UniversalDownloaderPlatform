using System;
using PuppeteerSharp;
using UniversalDownloaderPlatform.PuppeteerEngine.Interfaces.Wrappers.Browser;

namespace UniversalDownloaderPlatform.PuppeteerEngine.Wrappers.Browser
{
    /// <summary>
    /// This class is a wrapper around a Puppeteer Sharp's request object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    public sealed class WebRequest : IWebRequest
    {
        private readonly IRequest _request;

        public WebRequest(IRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public string Url => _request.Url;
    }
}
