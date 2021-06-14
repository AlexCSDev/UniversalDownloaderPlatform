using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using NLog;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace UniversalDownloaderPlatform.MegaDownloader
{
    internal class MegaFolder
    {
        public string Name;
        public string ParentId;
        public string Path;
    }

    internal class MegaCredentials
    {
        public string Email;
        public string Password;

        public MegaCredentials(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }

    internal class MegaDownloader : IDisposable
    {
        private MegaApiClient _client;

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly int _maxRetries = 10; //todo: load from IUniversalDownloaderPlatformSettings

        public MegaDownloader(MegaCredentials credentials = null)
        {
            _client = new MegaApiClient();
            if (credentials != null)
            {
                _client.Login(credentials.Email, credentials.Password);
            }
            else
            {
                _client.LoginAnonymous();
            }
        }

        public async Task DownloadUrlAsync(ICrawledUrl crawledUrl, string downloadPath, bool overwriteFiles = false)
        {
            _logger.Debug($"[MEGA] Staring downloading {crawledUrl.Url}");

            Uri uri = new Uri(crawledUrl.Url);

            if (await IsUrlAFolder(uri))
            {
                await DownloadFolderAsync(uri, Path.Combine(downloadPath, crawledUrl.DownloadPath), overwriteFiles);
            }
            else
            {
                INodeInfo fileNodeInfo = await _client.GetNodeFromLinkAsync(uri);
                string path = Path.Combine(downloadPath, crawledUrl.DownloadPath,
                    $"{fileNodeInfo.Id.Substring(0, 5)}_{fileNodeInfo.Name}");
                await DownloadFileAsync(null, uri, fileNodeInfo, path, overwriteFiles);
            }

            _logger.Debug($"[MEGA] Finished downloading {crawledUrl.Url}");
        }

        private async Task<bool> IsUrlAFolder(Uri uri)
        {
            //todo: replace with regex?
            try
            {
                IEnumerable<INode> nodes = await _client.GetNodesFromLinkAsync(uri);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task DownloadFolderAsync(Uri uri, string downloadPath, bool overwrite)
        {
            INode[] nodes = (await _client.GetNodesFromLinkAsync(uri)).ToArray();
            var folders = new List<KeyValuePair<string, MegaFolder>>();

            foreach (INode node in nodes)
            {
                if (folders.Any(x => x.Key == node.Id) || node.Type == NodeType.File)
                {
                    continue;
                }

                folders.Add(new KeyValuePair<string, MegaFolder>(node.Id,
                    new MegaFolder { Name = node.Name, ParentId = node.ParentId }));
            }

            foreach (var folder in folders)
            {
                var path = folder.Value.Name;
                var keyPath = folder.Key;
                var parentId = folder.Value.ParentId;
                while (parentId != null)
                {
                    var parentFolder = folders.FirstOrDefault(x => x.Key == parentId);
                    path = parentFolder.Value.Name + "/" + path;
                    keyPath = parentFolder.Key + "/" + keyPath;
                    parentId = parentFolder.Value.ParentId;
                }

                folder.Value.Path = path;
            }

            var rootFolder = folders.FirstOrDefault(x => string.IsNullOrEmpty(x.Value.ParentId));

            foreach (INode node in nodes.Where(x => x.Type == NodeType.File))
            {
                var path = Path.Combine(
                    downloadPath,
                    $"{rootFolder.Key.Substring(0, 5)}_{folders.FirstOrDefault(x => x.Key == node.ParentId).Value.Path}",
                    node.Name);
                try
                {
                    await DownloadFileAsync(node, null, null, path, overwrite);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error while downloading {path}: {ex}", ex);
                }
            }
        }

        private async Task DownloadFileAsync(INode fileNode, Uri fileUri, INodeInfo fileNodeInfo, string path, bool overwrite, int retry = 0) //MegaApiClient is a mess, that's why we pass so many parameters
        {
            if(fileNode == null && fileNodeInfo == null)
                throw new ArgumentException("fileNode or fileNodeInfo should be filled");
            if((fileNodeInfo != null && fileUri == null) || fileNodeInfo == null && fileUri != null)
                throw new ArgumentException("Both fileUri and fileNodeInfo should be filled");
            if (fileNode != null && fileNodeInfo != null)
                throw new ArgumentException("Both fileNode and fileNodeInfo cannot be filled at the same time");

            INodeInfo nodeInfo = fileNode != null ? fileNode : fileNodeInfo;
            if (nodeInfo.Type != NodeType.File)
                throw new Exception("Node is not a file");

            if (retry > 0)
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch (Exception fileDeleteException)
                {
                    throw new Common.Exceptions.DownloadException($"Unable to delete corrupted file {path}", fileDeleteException);
                }

                if (retry >= _maxRetries)
                {
                    throw new Common.Exceptions.DownloadException("Retries limit reached");
                }

                await Task.Delay(retry * 2 * 1000);
            }

            _logger.Debug($"[MEGA] Downloading {nodeInfo.Name} to {path}");

            long remoteFileSize = nodeInfo.Size;
            bool isFilesIdentical = false;

            if (File.Exists(path))
            {
                if (remoteFileSize > 0)
                {
                    _logger.Debug($"[MEGA] File {path} exists, size will be checked");
                    try
                    {
                        FileInfo fileInfo = new FileInfo(path);
                        long fileSize = fileInfo.Length;

                        if (fileSize != remoteFileSize)
                        {
                            string backupFilename =
                                    $"{Path.GetFileNameWithoutExtension(path)}_old_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{Path.GetExtension(path)}";
                            _logger.Warn($"[MEGA] Local and remote file sizes does not match, file {nodeInfo.Id} will be redownloaded. Old file will be backed up as {backupFilename}. Remote file size: {remoteFileSize}, local file size: {fileSize}");
                            File.Move(path, Path.Combine(fileInfo.DirectoryName, backupFilename));
                        }
                        else
                        {
                            _logger.Debug($"[MEGA] File size for {path} matches");
                            isFilesIdentical = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"[MEGA] Error during file comparison: {ex}");
                        isFilesIdentical = true; //we assume that local file is identical if we can't check remote file size
                    }
                }

                if (isFilesIdentical)
                {
                    if (!overwrite)
                    {
                        _logger.Warn($"[MEGA] File {path} already exists and has the same file size as remote file (or remote file is not available). Skipping...");
                        return;
                    }
                    else
                    {
                        _logger.Warn($"[MEGA] File {path} already exists, will be overwriten!");

                        try
                        {
                            File.Delete(path);
                        }
                        catch (Exception ex)
                        {
                            throw new Common.Exceptions.DownloadException($"Unable to delete file {path}", ex);
                        }
                    }
                }
            }

            try
            {
                //warning: returns '' in drive's root
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(new FileInfo(path).DirectoryName);
            }
            catch (Exception ex)
            {
                throw new Common.Exceptions.DownloadException($"Unable to create directory for file {path}", ex);
            }

            try
            {
                IProgress<double> progressHandler = new Progress<double>(x => _logger.Trace("Mega download progress: {0}%", x));
                if(fileNode != null)
                    await _client.DownloadFileAsync(fileNode, path, progressHandler);
                else
                    await _client.DownloadFileAsync(fileUri, path, progressHandler);

                FileInfo fileInfo = new FileInfo(path);
                long fileSize = fileInfo.Length;
                fileInfo = null;

                if (remoteFileSize > 0 && fileSize != remoteFileSize)
                {
                    _logger.Warn($"Downloaded file size differs from the size returned by server. Local size: {fileSize}, remote size: {remoteFileSize}. File {path} will be redownloaded.");

                    File.Delete(path);

                    retry++;

                    await DownloadFileAsync(fileNode, fileUri, fileNodeInfo, path, overwrite, retry);
                    return;
                }
                _logger.Debug($"File size check passed for: {path}");
            }
            catch (Exception ex)
            {
                retry++;
                _logger.Debug(ex, $"Encountered error while trying to download {nodeInfo.Id}, retrying in {retry * 2} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                await DownloadFileAsync(fileNode, fileUri, fileNodeInfo, path, overwrite, retry);
            }
        }

        ~MegaDownloader()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            _client.Logout();
            _client = null;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}
