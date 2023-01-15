using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalDownloaderPlatform.Common.Enums
{
    /// <summary>
    /// What to do when file with the same name already exists
    /// </summary>
    public enum FileExistsAction
    {
        /// <summary>
        /// Check remote file size if enabled and available. If it's different, disabled or not available then download remote file and compare it with existing file, create a backup copy of old file if they are different.
        /// </summary>
        BackupIfDifferent,
        /// <summary>
        /// Same as BackupIfDifferent, but the backup copy of the file will not be created.
        /// </summary>
        ReplaceIfDifferent,
        /// <summary>
        /// Always replace existing file
        /// </summary>
        AlwaysReplace,
        /// <summary>
        /// Always keep existing file
        /// </summary>
        KeepExisting
    }
}
