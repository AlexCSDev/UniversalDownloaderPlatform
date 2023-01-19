using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.PuppeteerEngine.Interfaces.Wrappers.Browser;

namespace UniversalDownloaderPlatform.PuppeteerEngine
{
    public interface IPuppeteerEngine : IDisposable
    {
        bool IsHeadless { get; }
        Task<IWebBrowser> GetBrowser();
        Task CloseBrowser();
    }
}
