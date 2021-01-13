namespace UniversalDownloaderPlatform.Common.Interfaces.Models
{
    public interface ICrawlTargetInfo
    {
        /// <summary>
        /// Directory which will be used to save the files. Can be either directory name or path relative to download directory.
        /// </summary>
        string SaveDirectory { get; }
    }
}
