using Ninject.Modules;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.PuppeteerEngine.Interfaces;
using UniversalDownloaderPlatform.PuppeteerEngine.Interfaces.Wrappers.Browser;
using UniversalDownloaderPlatform.PuppeteerEngine.Wrappers.Browser;

namespace UniversalDownloaderPlatform.PuppeteerEngine
{
    public class PuppeteerEngineModule : NinjectModule
    {
        public override void Load()
        {
            //Bind<IPuppeteerEngine>().To<PuppeteerEngine>().InSingletonScope();
            Rebind<ICookieRetriever>().To<PuppeteerCookieRetriever>().InSingletonScope();
            Rebind<ICaptchaSolver>().To<PuppeteerCaptchaSolver>().InSingletonScope();
            /*Bind<IWebBrowser>().To<WebBrowser>();
            Bind<IWebPage>().To<WebPage>();
            Bind<IWebRequest>().To<WebRequest>();
            Bind<IWebResponse>().To<WebResponse>();*/
        }
    }
}
