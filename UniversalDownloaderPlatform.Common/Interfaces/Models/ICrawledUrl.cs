namespace UniversalDownloaderPlatform.Common.Interfaces.Models
{
    public interface ICrawledUrl
    {
        string Url { get; set; }
        string Filename { get; set; }
        /// <summary>
        /// Download path relative to the download folder
        /// </summary>
        string DownloadPath { get; set; }
        /// <summary>
        /// Set to true if url was successfully downloaded
        /// </summary>
        bool IsDownloaded { get; set; }
    }
}
