using System;

namespace UniversalDownloaderPlatform.Common.Exceptions
{
    /// <summary>
    /// Base class for all UniversalDownloaderPlatform exceptions
    /// Thrown when there are no more specific exception is available
    /// </summary>
    public class UniversalDownloaderException : Exception
    {
        public UniversalDownloaderException() { }
        public UniversalDownloaderException(string message) : base(message) { }
        public UniversalDownloaderException(string message, Exception innerException) : base(message, innerException) { }
    }
}
