using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalDownloaderPlatform.Engine.Exceptions
{
    /// <summary>
    /// Thrown when there is an attempt to start download on instance of
    /// Universal Downloader Platform which is already downloading something
    /// </summary>
    public sealed class DownloaderAlreadyRunningException : UniversalDownloaderPlatformException
    {
        public DownloaderAlreadyRunningException() { }
        public DownloaderAlreadyRunningException(string message) : base(message) { }
        public DownloaderAlreadyRunningException(string message, Exception innerException) : base(message, innerException) { }
    }
}
