using System.Threading.Tasks;

namespace UniversalDownloaderPlatform.DefaultImplementations.Interfaces
{
    public interface IRemoteFileSizeChecker
    {
        Task<long> GetRemoteFileSize(string url);
    }
}
