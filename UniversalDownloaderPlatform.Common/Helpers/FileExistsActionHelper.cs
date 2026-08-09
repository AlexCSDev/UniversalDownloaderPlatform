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
        /// Resolves an already existing file path for the requested output path.
        /// Returns the exact path when it exists, otherwise looks for a file with the same basename and a different extension in the same directory.
        /// Returns null when no matching file exists.
        /// </summary>
        /// <param name="path">Requested output path</param>
        /// <returns>Existing file path or null when not found</returns>
        public static string ResolveExistingFilePath(string path)
        {
            if (File.Exists(path))
                return path;

            try
            {
                string directoryPath = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(directoryPath))
                    directoryPath = Directory.GetCurrentDirectory();
                if (!Directory.Exists(directoryPath))
                    return null;

                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(fileNameWithoutExtension))
                    return null;

                string requestedExtension = Path.GetExtension(path);
                // Enumerate all files and compare basenames in code so characters like * or ?
                // in the requested name are not treated as filesystem wildcards.
                foreach (string filePath in Directory.EnumerateFiles(directoryPath))
                {
                    if (string.Equals(Path.GetFileNameWithoutExtension(filePath), fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(Path.GetExtension(filePath), requestedExtension, StringComparison.OrdinalIgnoreCase))
                        return filePath;
                }
            }
            catch (Exception ex) when (ex is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or NotSupportedException
                or System.Security.SecurityException)
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Performs all required actions based on the FileExistsAction value. Should be called before downloading the file when the file already exists on the disk.
        /// When <paramref name="existingPath"/> is a same-basename converted equivalent of <paramref name="requestedPath"/>
        /// (different extension), the download is skipped unless <paramref name="fileExistsAction"/> is <see cref="FileExistsAction.AlwaysReplace"/>,
        /// because remote size/hash comparison is not meaningful across formats.
        /// </summary>
        /// <param name="existingPath">The path to the file already existing on the disk</param>
        /// <param name="requestedPath">The originally requested output path</param>
        /// <param name="remoteFileSize">The size of the remote file. Values less than or equal to 0 are treated as unavailable/unknown.</param>
        /// <param name="isCheckRemoteFileSize">Should the remote file size check be performed at all</param>
        /// <param name="fileExistsAction">Action to perform</param>
        /// <param name="loggingFunction">Logging function</param>
        /// <returns>True if should continue the download, false if should stop download process for the file</returns>
        public static bool DoFileExistsActionBeforeDownload(string existingPath,
            string requestedPath,
            long remoteFileSize,
            bool isCheckRemoteFileSize,
            FileExistsAction fileExistsAction,
            Action<LogMessageLevel, string, Exception> loggingFunction)
        {
            bool isConvertedEquivalent = !string.Equals(existingPath, requestedPath, StringComparison.OrdinalIgnoreCase);
            if (isConvertedEquivalent)
            {
                if (fileExistsAction == FileExistsAction.AlwaysReplace)
                    return true;

                loggingFunction(LogMessageLevel.Warning, $"Converted file {existingPath} already exists for requested path {requestedPath}, download will be skipped.", null);
                return false;
            }

            if (fileExistsAction != FileExistsAction.AlwaysReplace)
            {
                bool isFilesIdentical = false;
                if (isCheckRemoteFileSize)
                {
                    if (remoteFileSize > 0)
                    {
                        loggingFunction(LogMessageLevel.Debug, $"File {existingPath} exists, size will be checked", null);
                        try
                        {
                            if (new FileInfo(existingPath).Length != remoteFileSize)
                            {
                                loggingFunction(LogMessageLevel.Warning, $"Local and remote file sizes do not match, file {existingPath} will be redownloaded.", null);
                            }
                            else
                            {
                                loggingFunction(LogMessageLevel.Debug, $"File size for {existingPath} matches", null);
                                isFilesIdentical = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            loggingFunction(LogMessageLevel.Error, $"Error during file comparison: {ex}", ex);
                            // Leave isFilesIdentical false so ReplaceIfDifferent/BackupIfDifferent can fall back to after-download hash comparison.
                        }
                    }
                    // remoteFileSize <= 0 means unavailable/unknown; do not assume identity so after-download hash comparison can decide.
                }

                if (isFilesIdentical || fileExistsAction == FileExistsAction.KeepExisting)
                {
                    loggingFunction(LogMessageLevel.Warning, $"File {existingPath} already exists, will be skipped because of identical size to the remote file or because of file exists setting being set to keep existing file even on different remote size.", null);
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
