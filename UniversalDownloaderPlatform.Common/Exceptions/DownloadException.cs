using System;
using System.Net;

namespace UniversalDownloaderPlatform.Common.Exceptions
{
    /// <summary>
    /// Thrown when unrecoverable error is encountered during download process
    /// </summary>
    public sealed class DownloadException : UniversalDownloaderException
    {
        public HttpStatusCode? StatusCode { get; private set; }
        public string Response { get; private set; }

        public DownloadException() { }
        public DownloadException(string message) : base(message) { }

        public DownloadException(string message, HttpStatusCode statusCode, string response) : base(message)
        {
            StatusCode = statusCode;
            Response = response;
        }

        public DownloadException(string message, Exception innerException) : base(message, innerException) { }
    }
}
