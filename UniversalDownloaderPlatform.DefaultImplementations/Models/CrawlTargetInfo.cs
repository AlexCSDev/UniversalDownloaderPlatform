using System;
using System.Collections.Generic;
using System.Text;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.DefaultImplementations.Models
{
    public class CrawlTargetInfo : ICrawlTargetInfo
    {
        public string SaveDirectory { get; }
    }
}
