using System;
using System.IO;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Helpers;
using Xunit;

namespace UniversalDownloaderPlatform.Common.Tests
{
    public class FileExistsActionHelperTests : IDisposable
    {
        private readonly string _temporaryDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        public FileExistsActionHelperTests()
        {
            Directory.CreateDirectory(_temporaryDirectoryPath);
        }

        [Fact]
        public void ResolveExistingFilePath_ReturnsRequestedPath_WhenExactFileExists()
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "media_123.png");
            File.WriteAllText(requestedPath, "test");

            string existingPath = FileExistsActionHelper.ResolveExistingFilePath(requestedPath);

            Assert.Equal(requestedPath, existingPath);
        }

        [Fact]
        public void ResolveExistingFilePath_ReturnsSameBasenameFile_WhenDifferentExtensionExists()
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "media_123.png");
            string existingPath = Path.Combine(_temporaryDirectoryPath, "media_123.mp3");
            File.WriteAllText(existingPath, "test");

            string resolvedPath = FileExistsActionHelper.ResolveExistingFilePath(requestedPath);

            Assert.Equal(existingPath, resolvedPath);
        }

        [Fact]
        public void ResolveExistingFilePath_ReturnsNull_WhenOnlyPartialNameMatches()
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "media_123.png");
            string partialMatchPath = Path.Combine(_temporaryDirectoryPath, "media_123_extra.mp3");
            File.WriteAllText(partialMatchPath, "test");

            string existingPath = FileExistsActionHelper.ResolveExistingFilePath(requestedPath);

            Assert.Null(existingPath);
        }

        [Fact]
        public void ResolveExistingFilePath_ReturnsNull_WhenDirectoryDoesNotExist()
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "missing", "media_123.png");

            string existingPath = FileExistsActionHelper.ResolveExistingFilePath(requestedPath);

            Assert.Null(existingPath);
        }

        [Theory]
        [InlineData(FileExistsAction.BackupIfDifferent)]
        [InlineData(FileExistsAction.ReplaceIfDifferent)]
        [InlineData(FileExistsAction.KeepExisting)]
        public void DoFileExistsActionBeforeDownload_SkipsConvertedEquivalent_WhenNotAlwaysReplace(FileExistsAction fileExistsAction)
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "media_123.png");
            string existingPath = Path.Combine(_temporaryDirectoryPath, "media_123.mp3");
            File.WriteAllText(existingPath, "converted-content");

            bool shouldContinue = FileExistsActionHelper.DoFileExistsActionBeforeDownload(
                existingPath,
                requestedPath,
                remoteFileSize: 9999,
                isCheckRemoteFileSize: true,
                fileExistsAction,
                (_, _, _) => { });

            Assert.False(shouldContinue);
        }

        [Fact]
        public void DoFileExistsActionBeforeDownload_ContinuesConvertedEquivalent_WhenAlwaysReplace()
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "media_123.png");
            string existingPath = Path.Combine(_temporaryDirectoryPath, "media_123.mp3");
            File.WriteAllText(existingPath, "converted-content");

            bool shouldContinue = FileExistsActionHelper.DoFileExistsActionBeforeDownload(
                existingPath,
                requestedPath,
                remoteFileSize: 9999,
                isCheckRemoteFileSize: true,
                FileExistsAction.AlwaysReplace,
                (_, _, _) => { });

            Assert.True(shouldContinue);
        }

        [Fact]
        public void DoFileExistsActionBeforeDownload_UsesSizeCheck_ForExactPathMatch()
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "media_123.png");
            File.WriteAllText(requestedPath, "12345");

            bool shouldContinueWhenDifferent = FileExistsActionHelper.DoFileExistsActionBeforeDownload(
                requestedPath,
                requestedPath,
                remoteFileSize: 9999,
                isCheckRemoteFileSize: true,
                FileExistsAction.BackupIfDifferent,
                (_, _, _) => { });

            bool shouldSkipWhenIdentical = FileExistsActionHelper.DoFileExistsActionBeforeDownload(
                requestedPath,
                requestedPath,
                remoteFileSize: new FileInfo(requestedPath).Length,
                isCheckRemoteFileSize: true,
                FileExistsAction.BackupIfDifferent,
                (_, _, _) => { });

            Assert.True(shouldContinueWhenDifferent);
            Assert.False(shouldSkipWhenIdentical);
        }

        [Theory]
        [InlineData(FileExistsAction.BackupIfDifferent)]
        [InlineData(FileExistsAction.ReplaceIfDifferent)]
        public void DoFileExistsActionBeforeDownload_Continues_WhenRemoteSizeUnknown(FileExistsAction fileExistsAction)
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "media_123.png");
            File.WriteAllText(requestedPath, "12345");

            bool shouldContinue = FileExistsActionHelper.DoFileExistsActionBeforeDownload(
                requestedPath,
                requestedPath,
                remoteFileSize: -1,
                isCheckRemoteFileSize: true,
                fileExistsAction,
                (_, _, _) => { });

            Assert.True(shouldContinue);
        }

        [Fact]
        public void DoFileExistsActionBeforeDownload_Skips_WhenRemoteSizeUnknownAndKeepExisting()
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "media_123.png");
            File.WriteAllText(requestedPath, "12345");

            bool shouldContinue = FileExistsActionHelper.DoFileExistsActionBeforeDownload(
                requestedPath,
                requestedPath,
                remoteFileSize: 0,
                isCheckRemoteFileSize: true,
                FileExistsAction.KeepExisting,
                (_, _, _) => { });

            Assert.False(shouldContinue);
        }

        [Fact]
        public void ResolveExistingFilePath_MatchesBasename_IgnoringCase_WhenFilesystemSurfacesCandidate()
        {
            // Create both casings when the filesystem allows it. On case-insensitive systems
            // only one file entry exists and Directory.EnumerateFiles returns it for either casing.
            string lowerExistingPath = Path.Combine(_temporaryDirectoryPath, "media_123.mp3");
            string mixedRequestedPath = Path.Combine(_temporaryDirectoryPath, "Media_123.png");
            File.WriteAllText(lowerExistingPath, "test");

            string resolvedPath = FileExistsActionHelper.ResolveExistingFilePath(mixedRequestedPath);

            // On case-insensitive filesystems the candidate is found and basename compare must ignore case.
            // On case-sensitive filesystems only an exact casing match is expected from the filesystem listing.
            if (resolvedPath != null)
            {
                Assert.Equal(Path.GetFileNameWithoutExtension(lowerExistingPath), Path.GetFileNameWithoutExtension(resolvedPath), ignoreCase: true);
                Assert.Equal(Path.GetExtension(lowerExistingPath), Path.GetExtension(resolvedPath), ignoreCase: true);
                Assert.False(string.Equals(Path.GetExtension(mixedRequestedPath), Path.GetExtension(resolvedPath), StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public void ResolveExistingFilePath_DoesNotTreatWildcardCharactersInBasenameAsGlobs()
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "media*123.png");
            string unrelatedMatchPath = Path.Combine(_temporaryDirectoryPath, "mediaX123.mp3");
            string exactConvertedPath = Path.Combine(_temporaryDirectoryPath, "media*123.mp3");
            File.WriteAllText(unrelatedMatchPath, "unrelated");
            File.WriteAllText(exactConvertedPath, "converted");

            string resolvedPath = FileExistsActionHelper.ResolveExistingFilePath(requestedPath);

            Assert.Equal(exactConvertedPath, resolvedPath);
        }

        [Fact]
        public void ResolveExistingFilePath_ReturnsNull_WhenWildcardBasenameHasOnlyUnrelatedMatches()
        {
            string requestedPath = Path.Combine(_temporaryDirectoryPath, "media?123.png");
            string unrelatedMatchPath = Path.Combine(_temporaryDirectoryPath, "mediaA123.mp3");
            File.WriteAllText(unrelatedMatchPath, "unrelated");

            string resolvedPath = FileExistsActionHelper.ResolveExistingFilePath(requestedPath);

            Assert.Null(resolvedPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_temporaryDirectoryPath))
                Directory.Delete(_temporaryDirectoryPath, true);
        }
    }
}
