﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace UniversalDownloaderPlatform.Common.Events
{
    public enum CrawlerMessageType
    {
        Info,
        Warning,
        Error
    }
    public sealed class CrawlerMessageEventArgs : EventArgs
    {
        private readonly CrawlerMessageType _messageType;
        private readonly string _message;
        private readonly string _postId;

        public CrawlerMessageType MessageType => _messageType;
        public string Message => _message;

        public string PostId => _postId;

        public CrawlerMessageEventArgs(CrawlerMessageType messageType, string message, string postId = "unknown")
        {
            _messageType = messageType;
            _message = message ?? throw new ArgumentNullException(nameof(message));
            _postId = postId;
        }
    }
}
