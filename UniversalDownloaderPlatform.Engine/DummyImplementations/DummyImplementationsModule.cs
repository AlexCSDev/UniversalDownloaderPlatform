using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.DefaultImplementations;
using UniversalDownloaderPlatform.Engine.DependencyInjection;
using UniversalDownloaderPlatform.Engine.Helpers;
using UniversalDownloaderPlatform.Engine.Interfaces;

namespace UniversalDownloaderPlatform.Engine.DummyImplementations
{
    internal class DummyImplementationsModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ICookieRetriever>().To<DummyCookieRetriever>().InSingletonScope();
            Bind<ICaptchaSolver>().To<DummyCaptchaSolver>().InSingletonScope();
            Bind<ICookieValidator>().To<DummyCookieValidator>().InSingletonScope();
            Bind<ICrawlResultsExporter>().To<DummyCrawlResultsExporter>().InSingletonScope();
        }
    }
}
