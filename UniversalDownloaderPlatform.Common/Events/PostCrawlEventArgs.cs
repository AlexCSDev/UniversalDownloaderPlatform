using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalDownloaderPlatform.Common.Events
{
    public sealed class PostCrawlEventArgs : EventArgs
    {
        private readonly string _postId;
        private readonly bool _success;
        private readonly string _errorMessage;

        public string PostId => _postId;
        public bool Success => _success;
        public string ErrorMessage => _errorMessage;

        public PostCrawlEventArgs(string postId, bool success, string errorMessage = null)
        {
            _postId = postId != null ? postId : throw new ArgumentNullException(nameof(postId), "Value cannot be null");
            _success = success;
            if (!success)
                _errorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage), "Value could not be null if success is false");
        }
    }
}
