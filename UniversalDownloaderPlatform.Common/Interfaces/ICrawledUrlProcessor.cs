using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Interfaces
{
    public interface ICrawledUrlProcessor
    {
        /// <summary>
        /// Do any additional processing on the crawled url before download process starts
        /// </summary>
        /// <param name="crawledUrl">Crawled url</param>
        /// <returns></returns>
        Task ProcessCrawledUrl(ICrawledUrl crawledUrl, string downloadDirectory);
    }
}
