using System;
using System.Collections.Generic;
using System.Text;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.Common.Events
{
    public sealed class NewCrawledUrlEventArgs : EventArgs
    {
        private readonly ICrawledUrl _crawledUrl;

        public ICrawledUrl CrawledUrl => _crawledUrl;

        public NewCrawledUrlEventArgs(ICrawledUrl crawledUrl)
        {
            _crawledUrl = crawledUrl ?? throw new ArgumentNullException(nameof(crawledUrl));
        }
    }
}
