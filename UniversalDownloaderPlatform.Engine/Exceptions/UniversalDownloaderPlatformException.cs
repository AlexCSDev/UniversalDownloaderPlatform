using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalDownloaderPlatform.Engine.Exceptions
{
    /// <summary>
    /// Base class for all Universal Downloader Platform exceptions
    /// Thrown when there are no more specific exception is available
    /// </summary>
    public class UniversalDownloaderPlatformException : Exception
    {
        public UniversalDownloaderPlatformException() { }
        public UniversalDownloaderPlatformException(string message) : base(message) { }
        public UniversalDownloaderPlatformException(string message, Exception innerException) : base(message, innerException) { }
    }
}
