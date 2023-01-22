using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.DefaultImplementations.Interfaces
{
    public interface IRemoteFileInfoRetriever
    {
        /// <summary>
        /// Initialization function, called on every UniversalDownloader.Download call
        /// </summary>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);
        /// <summary>
        /// Retrieve name and size of remote file
        /// </summary>
        /// <returns></returns>
        public Task<(string, long)> GetRemoteFileInfo(string url, bool useMediaType, string refererUrl = null);
    }
}
