using System;

namespace UniversalDownloaderPlatform.Common.Exceptions
{
    /// <summary>
    /// Thrown when supplied cookies are invalid or incomplete
    /// </summary>
    public sealed class CookieValidationException : UniversalDownloaderException
    {
        public CookieValidationException() { }
        public CookieValidationException(string message) : base(message) { }
        public CookieValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
