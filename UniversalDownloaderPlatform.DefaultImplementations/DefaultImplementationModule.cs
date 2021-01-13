using System;
using System.Collections.Generic;
using System.Text;
using Ninject.Modules;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    public class DefaultImplementationModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IRemoteFileSizeChecker>().To<RemoteFileSizeChecker>().InSingletonScope();
            Bind<IWebDownloader>().To<WebDownloader>().InSingletonScope();
        }
    }
}
