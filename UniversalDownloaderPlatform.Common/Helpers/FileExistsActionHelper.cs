using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Exceptions;

namespace UniversalDownloaderPlatform.Common.Helpers
{
    public static class FileExistsActionHelper
    {
        /// <summary>
        /// Performs all required actions based on the FileExistsAction value. Should be called before downloading the file when the file already exists on the disk.
        /// </summary>
        /// <param name="path">The path to the file already existing on the disk</param>
        /// <param name="remoteFileSize">The size of the remote file (supply -1 if not available)</param>
        /// <param name="isCheckRemoteFileSize">Should the remote file size check be performed at all</param>
        /// <param name="fileExistsAction">Action to perform</param>
        /// <param name="loggingFunction">Logging function</param>
        /// <returns>True if should continue the download, false if should stop download process for the file</returns>
        public static bool DoFileExistsActionBeforeDownload(string path,
            long remoteFileSize,
            bool isCheckRemoteFileSize,
            FileExistsAction fileExistsAction,
            Action<LogMessageLevel, string, Exception> loggingFunction)
        {
            if (fileExistsAction != FileExistsAction.AlwaysReplace)
            {
                bool isFilesIdentical = false;
                if (isCheckRemoteFileSize)
                {
                    if (remoteFileSize > 0)
                    {
                        loggingFunction(LogMessageLevel.Debug, $"File {path} exists, size will be checked", null);
                        try
                        {
                            if (new FileInfo(path).Length != remoteFileSize)
                            {
                                loggingFunction(LogMessageLevel.Warning, $"Local and remote file sizes does not match, file {path} will be redownloaded.", null);
                            }
                            else
                            {
                                loggingFunction(LogMessageLevel.Debug, $"File size for {path} matches", null);
                                isFilesIdentical = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            loggingFunction(LogMessageLevel.Error, $"Error during file comparison: {ex}", ex);
                            isFilesIdentical = true; //we assume that local file is identical if we can't check remote file size
                        }
                    }
                    else
                        isFilesIdentical = true; //assume that 0kb files and failed checks are always identical
                }

                if (isFilesIdentical || fileExistsAction == FileExistsAction.KeepExisting)
                {
                    loggingFunction(LogMessageLevel.Warning, $"File {path} already exists, will be skipped because of identical size to the remote file or because of file exists setting being set to keep existing file even on different remote size.", null);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Performs all required actions based on the FileExistsAction value. Should be called after temporary file has been downloaded when the file already exists on the disk.
        /// Automatically moves temporary file to the proper path
        /// </summary>
        /// <param name="path">The path to the file already existing on the disk</param>
        /// <param name="temporaryFilePath">The path to the temporary file on the disk</param>
        /// <param name="fileExistsAction">Action to perform</param>
        /// <param name="loggingFunction">Logging function</param>
        /// <exception cref="Exception"></exception>
        public static void DoFileExistsActionAfterDownload(
            string path,
            string temporaryFilePath,
            FileExistsAction fileExistsAction,
            Action<LogMessageLevel, string, Exception> loggingFunction)
        {
            if(File.Exists(path))
            {
                bool isShouldRemoveExistingFile = false;
                if (fileExistsAction == FileExistsAction.ReplaceIfDifferent ||
                    fileExistsAction == FileExistsAction.BackupIfDifferent)
                {
                    string existingFileHash = FileHashHelper.CalculateFileHash(path).ToHex(true);
                    string downloadedFileHash = FileHashHelper.CalculateFileHash(temporaryFilePath).ToHex(true);

                    if (existingFileHash != downloadedFileHash)
                    {
                        if (fileExistsAction == FileExistsAction.BackupIfDifferent)
                        {
                            string backupFilename =
                                    $"{Path.GetFileNameWithoutExtension(path)}_old_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{Path.GetExtension(path)}";
                            loggingFunction(LogMessageLevel.Warning, $"Local and remote files are different, file {Path.GetFileName(path)} will replaced. Old file will be backed up as {Path.GetFileName(backupFilename)}. Remote file hash: {downloadedFileHash}, local file hash: {existingFileHash}", null);
                            File.Move(path, Path.Combine(Path.GetDirectoryName(path), backupFilename));
                        }
                        else
                        {
                            isShouldRemoveExistingFile = true;
                        }
                    }
                    else
                    {
                        loggingFunction(LogMessageLevel.Information, $"Existing file {Path.GetFileName(path)} is identical to downloaded file, original file will be kept.", null);
                        try
                        {
                            File.Delete(temporaryFilePath);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Unable to remove the temporary file {Path.GetFileName(temporaryFilePath)} because of it being identical to existing file, error: {ex}", ex);
                        }
                        return;
                    }
                }
                else if (fileExistsAction == FileExistsAction.AlwaysReplace)
                {
                    isShouldRemoveExistingFile = true;
                }
                else //safeguard
                {
                    throw new Exception($"Invalid state for {Path.GetFileName(path)}, managed to get past all FileExistActions check. Contact developer. Leftover files might be present in the download directory.");
                }

                if (isShouldRemoveExistingFile)
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Unable to remove the original file {Path.GetFileName(path)} in order to replace with temporary file {Path.GetFileName(temporaryFilePath)}, error: {ex}", ex);
                    }
                }
            }

            try
            {
                File.Move(temporaryFilePath, path);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to move {Path.GetFileName(temporaryFilePath)} to {Path.GetFileName(path)}, error: {ex}", ex);
            }
        }
    }
}
