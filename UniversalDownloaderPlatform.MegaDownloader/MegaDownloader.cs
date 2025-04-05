using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;
using NLog;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Helpers;
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

        private int _maxRetries = 10;
        private bool _isCheckRemoteFileSize;

        public MegaDownloader(MegaCredentials credentials = null)
        {
            _client = new MegaApiClient();

            try
            {
                if (credentials != null)
                {
                    _client.Login(credentials.Email, credentials.Password);
                }
                else
                {
                    _client.LoginAnonymous();
                }
            }
            catch(HttpRequestException reqEx)
            {
                if(reqEx.StatusCode == System.Net.HttpStatusCode.PaymentRequired)
                {
                    _logger.Fatal("[MEGA] Mega.nz plugin cannot log in using supplied credentials. Please check supplied username and password, then sign in into your account using your browser and you will be able to download files for the next 24 hours." +
                        "\r\nPlease refer to this issue for more information: https://github.com/AlexCSDev/PatreonDownloader/issues/257" +
                        "\r\nYou can ignore this message if you do not plan to download files from mega.nz.");
                    return;
                }

                _logger.Fatal(reqEx, $"[MEGA] Mega.nz plugin cannot log in using supplied credentials, mega.nz files WILL NOT BE DOWNLOADED. Reason: {reqEx}");
            }
            catch(Exception ex)
            {
                _logger.Fatal(ex, $"[MEGA] Mega.nz plugin cannot log in using supplied credentials, mega.nz files WILL NOT BE DOWNLOADED. Reason: {ex}");
            }
        }

        public void BeforeStart(int maxRetries, bool isCheckRemoteFileSize)
        {
            _maxRetries = maxRetries;
            _isCheckRemoteFileSize = isCheckRemoteFileSize;
        }

        public async Task DownloadUrlAsync(ICrawledUrl crawledUrl, string downloadPath, FileExistsAction fileExistsAction)
        {
            if(_client == null || !_client.IsLoggedIn)
                throw new UniversalDownloaderPlatform.Common.Exceptions.DownloadException($"{crawledUrl.Url} cannot be downloaded because mega plugin is not logged in or was unable to initialize.");

            _logger.Debug($"[MEGA] Staring downloading {crawledUrl.Url}");

            Uri uri = new Uri(crawledUrl.Url);

            if (await IsUrlAFolder(uri))
            {
                await DownloadFolderAsync(uri, Path.Combine(downloadPath, crawledUrl.DownloadPath), fileExistsAction);
            }
            else
            {
                (_, string id, _) = MegaUrlDataExtractor.Extract(crawledUrl.Url);
                INode fileNodeInfo = await _client.GetNodeFromLinkAsync(uri);
                string path = Path.Combine(downloadPath, crawledUrl.DownloadPath,
                    $"{id.Substring(0, 5)}_{fileNodeInfo.Name}");
                await DownloadFileAsync(null, uri, fileNodeInfo, path, fileExistsAction);
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

        private async Task DownloadFolderAsync(Uri uri, string downloadPath, FileExistsAction fileExistsAction)
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
                    new MegaFolder { Name = node.Name.Trim(), ParentId = node.ParentId }));
            }

            foreach (var folder in folders)
            {
                var path = folder.Value.Name;
                var keyPath = folder.Key;
                var parentId = folder.Value.ParentId;
                while (parentId != null)
                {
                    var parentFolder = folders.FirstOrDefault(x => x.Key == parentId);
                    path = Path.Combine(parentFolder.Value.Name, path);
                    keyPath = Path.Combine(parentFolder.Key, keyPath);
                    parentId = parentFolder.Value.ParentId;
                }

                folder.Value.Path = path;
            }

            (_, string id, _) = MegaUrlDataExtractor.Extract(uri.ToString());

            foreach (INode node in nodes.Where(x => x.Type == NodeType.File))
            {
                var path = Path.Combine(
                    downloadPath,
                    $"{id.Substring(0, 5)}_{folders.FirstOrDefault(x => x.Key == node.ParentId).Value.Path}",
                    node.Name);
                try
                {
                    await DownloadFileAsync(node, null, null, path, fileExistsAction);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error while downloading {path}: {ex}");
                }
            }
        }

        private async Task DownloadFileAsync(INode fileNode, Uri fileUri, INode fileNodeInfo, string path, FileExistsAction fileExistsAction, int retry = 0) //MegaApiClient is a mess, that's why we pass so many parameters
        {
            if(fileNode == null && fileNodeInfo == null)
                throw new ArgumentException("fileNode or fileNodeInfo should be filled");
            if((fileNodeInfo != null && fileUri == null) || fileNodeInfo == null && fileUri != null)
                throw new ArgumentException("Both fileUri and fileNodeInfo should be filled");
            if (fileNode != null && fileNodeInfo != null)
                throw new ArgumentException("Both fileNode and fileNodeInfo cannot be filled at the same time");

            INode nodeInfo = fileNode != null ? fileNode : fileNodeInfo;
            if (nodeInfo.Type != NodeType.File)
                throw new Exception("Node is not a file");

            string temporaryFilePath = $"{path}.dwnldtmp";

            try
            {
                if (File.Exists(temporaryFilePath))
                    File.Delete(temporaryFilePath);
            }
            catch (Exception fileDeleteException)
            {
                throw new Common.Exceptions.DownloadException($"Unable to delete existing temporary file {temporaryFilePath}", fileDeleteException);
            }

            if (retry > 0)
            {

                if (retry >= _maxRetries)
                {
                    throw new Common.Exceptions.DownloadException("Retries limit reached");
                }

                await Task.Delay(retry * 2 * 1000);
            }

            _logger.Debug($"[MEGA] Downloading {nodeInfo.Name} to {path}");

            long remoteFileSize = nodeInfo.Size;

            if (File.Exists(path))
            {
                if (!FileExistsActionHelper.DoFileExistsActionBeforeDownload(path, remoteFileSize, _isCheckRemoteFileSize, fileExistsAction, LoggingFunction))
                    return;
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
                    await _client.DownloadFileAsync(fileNode, temporaryFilePath, progressHandler);
                else
                    await _client.DownloadFileAsync(fileUri, temporaryFilePath, progressHandler);

                FileInfo fileInfo = new FileInfo(temporaryFilePath);
                long fileSize = fileInfo.Length;
                fileInfo = null;

                if (remoteFileSize > 0 && fileSize != remoteFileSize)
                {
                    _logger.Warn($"[MEGA] Downloaded file size differs from the size returned by server. Local size: {fileSize}, remote size: {remoteFileSize}. File {path} will be redownloaded.");

                    File.Delete(temporaryFilePath);

                    retry++;

                    await DownloadFileAsync(fileNode, fileUri, fileNodeInfo, path, fileExistsAction, retry);
                    return;
                }
                _logger.Debug($"[MEGA] File size check passed for: {path}");

                _logger.Debug($"[MEGA] Renaming temporary file for: {path}");

                try
                {
                    FileExistsActionHelper.DoFileExistsActionAfterDownload(path, temporaryFilePath, fileExistsAction, LoggingFunction);
                }
                catch (Exception ex)
                {
                    throw new Common.Exceptions.DownloadException(ex.Message, ex);
                }
            }
            catch (Exception ex)
            {
                if (ex is Common.Exceptions.DownloadException)
                    throw;

                retry++;
                _logger.Error(ex, $"Encountered error while trying to download {nodeInfo.Id}, retrying in {retry * 2} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                await DownloadFileAsync(fileNode, fileUri, fileNodeInfo, path, fileExistsAction, retry);
            }
        }

        /// <summary>
        /// Logging function for FileExistsActionHelper calls
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        private void LoggingFunction(LogMessageLevel level, string message, Exception exception)
        {
            message = $"[MEGA] {message}";
            switch (level)
            {
                case LogMessageLevel.Trace:
                    _logger.Trace(exception, message);
                    break;
                case LogMessageLevel.Debug:
                    _logger.Debug(exception, message);
                    break;
                case LogMessageLevel.Fatal:
                    _logger.Fatal(exception, message);
                    break;
                case LogMessageLevel.Error:
                    _logger.Error(exception, message);
                    break;
                case LogMessageLevel.Warning:
                    _logger.Warn(exception, message);
                    break;
                case LogMessageLevel.Information:
                    _logger.Info(exception, message);
                    break;
            }
        }

        ~MegaDownloader()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            if (_client == null)
                return;

            if(_client.IsLoggedIn)
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
