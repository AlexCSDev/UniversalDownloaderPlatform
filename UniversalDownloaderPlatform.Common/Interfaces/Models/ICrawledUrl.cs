namespace UniversalDownloaderPlatform.Common.Interfaces.Models
{
    public interface ICrawledUrl
    {
        string Url { get; set; }
        string Filename { get; set; }
    }
}
