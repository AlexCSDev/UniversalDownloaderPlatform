using System;
using System.Collections.Generic;
using System.Text;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.DefaultImplementations.Models
{
    public class CrawledUrl : ICrawledUrl
    {
        public string Url { get; set; }
        public string Filename { get; set; }
        public string DownloadPath { get; set; }
        public bool IsDownloaded { get; set; }
        public bool IsProcessedByPlugin { get; set; }
    }
}
