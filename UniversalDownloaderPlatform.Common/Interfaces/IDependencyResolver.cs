using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface IDependencyResolver
    {
        /// <summary>
        /// Get instance of the class, returns null if nothing found.
        /// </summary>
        /// <typeparam name="T">Class/Interface</typeparam>
        /// <returns></returns>
        public T Get<T>();
    }
}
