using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Engine.DummyImplementations
{
    internal class DummyCrawlResultsExporter : ICrawlResultsExporter
    {
        public Task ExportCrawlResults(ICrawlTargetInfo crawlTargetInfo, List<ICrawledUrl> crawledUrls)
        {
            return Task.CompletedTask;
        }
    }
}
