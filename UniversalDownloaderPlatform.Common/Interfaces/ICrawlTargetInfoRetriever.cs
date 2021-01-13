using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface ICrawlTargetInfoRetriever
    {
        Task<ICrawlTargetInfo> RetrieveCrawlTargetInfo(string url);
    }
}
