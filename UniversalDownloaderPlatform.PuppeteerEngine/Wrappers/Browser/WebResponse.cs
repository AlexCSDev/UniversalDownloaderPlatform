using System;
using System.Net;
using System.Threading.Tasks;
using PuppeteerSharp;
using UniversalDownloaderPlatform.PuppeteerEngine.Interfaces.Wrappers.Browser;

namespace UniversalDownloaderPlatform.PuppeteerEngine.Wrappers.Browser
{
    /// <summary>
    /// This class is a wrapper around a Puppeteer Sharp's response object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    public sealed class WebResponse : IWebResponse
    {
        private readonly IResponse _response;
        public WebResponse(IResponse response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public HttpStatusCode Status
        {
            get { return _response.Status; }
        }
        
        public async Task<string> TextAsync()
        {
            return await _response.TextAsync();
        }
    }
}
