using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces;

namespace UniversalDownloaderPlatform.Engine.DependencyInjection
{
    /// <summary>
    /// Simple wrapper around Ninject in order to not force plugins and common library to reference ninject
    /// </summary>
    public class DependencyResolver : IDependencyResolver
    {
        private readonly IKernel _kernel;
        public DependencyResolver(IKernel kernel) 
        { 
            _kernel = kernel;
        }

        public T Get<T>()
        {
            return _kernel.TryGet<T>();
        }
    }
}
