using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Ninject;
using Ninject.Modules;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.Engine.DummyImplementations;
using UniversalDownloaderPlatform.Engine.Helpers;
using UniversalDownloaderPlatform.Engine.Interfaces;

namespace UniversalDownloaderPlatform.Engine.DependencyInjection
{
    public class MainModule : NinjectModule
    {
        public override void Load()
        {
            Kernel.Load<DummyImplementationsModule>();

            Bind<IPluginManager>().To<PluginManager>().InSingletonScope();
            Bind<IUrlChecker>().To<UrlChecker>().InSingletonScope();
            Bind<IDependencyResolver>().To<DependencyResolver>().InSingletonScope();
            Bind<IDownloadManager>().To<DownloadManager>();

            //Kernel.Load("PatreonDownloader.PuppeteerEngine.dll");
        }
    }
}
