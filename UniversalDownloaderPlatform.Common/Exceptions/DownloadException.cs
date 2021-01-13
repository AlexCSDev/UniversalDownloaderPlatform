﻿using System;

namespace UniversalDownloaderPlatform.Common.Exceptions
{
    /// <summary>
    /// Thrown when unrecoverable error is encountered during download process
    /// </summary>
    public sealed class DownloadException : UniversalDownloaderException
    {
        public DownloadException() { }
        public DownloadException(string message) : base(message) { }
        public DownloadException(string message, Exception innerException) : base(message, innerException) { }
    }
}
