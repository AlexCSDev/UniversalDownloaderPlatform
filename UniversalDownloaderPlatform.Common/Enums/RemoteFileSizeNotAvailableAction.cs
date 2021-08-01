using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalDownloaderPlatform.Common.Enums
{
    /// <summary>
    /// What to do when remote file size is not available
    /// </summary>
    public enum RemoteFileSizeNotAvailableAction
    {
        /*/// <summary>
        /// Download remote file into temporary folder and compare it with existing file
        /// </summary>
        DownloadAndCompare,*/
        /// <summary>
        /// Replace existing file
        /// </summary>
        ReplaceExisting,
        /// <summary>
        /// Keep existing file
        /// </summary>
        KeepExisting
    }
}
