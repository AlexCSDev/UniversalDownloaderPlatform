using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.DefaultImplementations.Interfaces
{
    public interface IRemoteFileSizeChecker
    {
        /// <summary>
        /// Initialization function, called on every UniversalDownloader.Download call
        /// </summary>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);
        /// <summary>
        /// Get size in bytes of the remote file. Returns 0 if not available.
        /// </summary>
        /// <param name="url">Url of the remote file</param>
        /// <param name="refererUrl">Url to be placed into the referer header, can be null</param>
        /// <returns></returns>
        Task<long> GetRemoteFileSize(string url, string refererUrl = null);
    }
}
