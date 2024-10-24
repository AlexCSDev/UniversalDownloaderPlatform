﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Logging;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using HeyRed.Mime;
using NLog;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Helpers;
using File = Google.Apis.Drive.v3.Data.File;

namespace UniversalDownloaderPlatform.GoogleDriveDownloader
{
    internal class GoogleDriveEngine
    {
        private static readonly string[] Scopes = { DriveService.Scope.DriveReadonly };
        private static readonly string ApplicationName = "PatreonDownloader Google Drive Plugin";

        private static readonly DriveService Service;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static GoogleDriveEngine()
        {
            if (!System.IO.File.Exists("gd_credentials.json"))
                return;

            UserCredential credential;
            using (var stream =
                new FileStream("gd_credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "GoogleDriveToken";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Logger.Debug("Token data saved to: " + credPath);
            }

            // Create Drive API service.
            Service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public void Download(string id, string path, FileExistsAction fileExistsAction, bool isCheckRemoteFileSize)
        {
            if (Service == null)
                return;

            File fileResource = Service.Files.Get(id).Execute();

            DownloadFileResource(fileResource, path, fileExistsAction, isCheckRemoteFileSize, true);
        }

        private void DownloadFileResource(File fileResource, string path, FileExistsAction fileExistsAction, bool isCheckRemoteFileSize, bool rootPath = true)
        {
            string sanitizedFilename = PathSanitizer.SanitizePath(fileResource.Name).Trim();

            if (rootPath)
            {
                path += sanitizedFilename.Trim();
            }
            else
            {
                path = Path.Combine(path, sanitizedFilename);
            }

            string temporaryFilePath = $"{path}.dwnldtmp";

            try
            {
                if (System.IO.File.Exists(temporaryFilePath))
                    System.IO.File.Delete(temporaryFilePath);
            }
            catch (Exception fileDeleteException)
            {
                throw new DownloadException($"Unable to delete existing temporary file {temporaryFilePath}", fileDeleteException);
            }

            Logger.Info($"[Google Drive] Downloading {fileResource.Name}");
            Logger.Debug($"[Google Drive] {fileResource.Name} mime type is: {fileResource.MimeType}");

            if (fileResource.MimeType != "application/vnd.google-apps.folder")
            {
                long? remoteFileSize = fileResource.Size;

                if (System.IO.File.Exists(path))
                {
                    if (!FileExistsActionHelper.DoFileExistsActionBeforeDownload(path, remoteFileSize ?? 0, isCheckRemoteFileSize, fileExistsAction, LoggingFunction))
                        return;
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(new FileInfo(path).DirectoryName);
                }

                //todo: allow choosing which format to use: pdf or office
                bool isGoogleDocument = false;
                string mimeType = null;
                // https://developers.google.com/drive/api/v3/mime-types
                // https://developers.google.com/drive/api/v3/ref-export-formats
                if (fileResource.MimeType == "application/vnd.google-apps.document" ||
                    fileResource.MimeType == "application/vnd.google-apps.spreadsheet" ||
                    fileResource.MimeType == "application/vnd.google-apps.presentation")
                {
                    /*string extension = Path.GetExtension(path);
                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        path += ".pdf";
                        mimeType = "application/pdf";
                    }
                    else
                        mimeType = MimeTypesMap.GetMimeType(extension);*/
                    switch(fileResource.MimeType)
                    {
                        case "application/vnd.google-apps.document":
                            path += ".docx";
                            mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                            break;
                        case "application/vnd.google-apps.spreadsheet":
                            path += ".xlsx";
                            mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            break;
                        case "application/vnd.google-apps.presentation":
                            path += ".pptx";
                            mimeType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                            break;
                    }

                    isGoogleDocument = true;
                }

                using (FileStream file = new FileStream(temporaryFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (isGoogleDocument)
                    {
                        Service.Files.Export(fileResource.Id, mimeType).Download(file);
                    }
                    else
                    {
                        IDownloadProgress downloadProgress = Service.Files.Get(fileResource.Id).DownloadWithStatus(file);
                        if(downloadProgress.Status == DownloadStatus.Failed)
                            throw new Exception($"Unable to download {fileResource.Name}: {downloadProgress.Exception}");
                    }
                }

                try
                {
                    FileExistsActionHelper.DoFileExistsActionAfterDownload(path, temporaryFilePath, fileExistsAction, LoggingFunction);
                }
                catch (Exception ex)
                {
                    throw new Common.Exceptions.DownloadException(ex.Message, ex);
                }
            }
            else
            {

                Directory.CreateDirectory(path);
                var subFolderItems = RessInFolder(fileResource.Id);

                foreach (var item in subFolderItems)
                    DownloadFileResource(item, path, fileExistsAction, isCheckRemoteFileSize, false);
            }
        }

        private List<File> RessInFolder(string folderId)
        {
            Logger.Info($"[Google Drive] Scanning folder {folderId}");
            List<File> retList = new List<File>();
            var request = Service.Files.List();

            request.Q = $"'{folderId}' in parents";

            do
            {
                var children = request.Execute();

                foreach (File child in children.Files)
                {
                    Logger.Info($"[Google Drive] Found file {child.Name} in folder {folderId}");
                    retList.Add(Service.Files.Get(child.Id).Execute());
                }

                request.PageToken = children.NextPageToken;
            } while (!string.IsNullOrWhiteSpace(request.PageToken));

            Logger.Info($"[Google Drive] Finished scanning folder {folderId}");
            return retList;
        }

        /// <summary>
        /// Logging function for FileExistsActionHelper calls
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        private void LoggingFunction(LogMessageLevel level, string message, Exception exception)
        {
            message = $"[Google Drive] {message}";
            switch (level)
            {
                case LogMessageLevel.Trace:
                    Logger.Trace(message, exception);
                    break;
                case LogMessageLevel.Debug:
                    Logger.Debug(message, exception);
                    break;
                case LogMessageLevel.Fatal:
                    Logger.Fatal(message, exception);
                    break;
                case LogMessageLevel.Error:
                    Logger.Error(message, exception);
                    break;
                case LogMessageLevel.Warning:
                    Logger.Warn(message, exception);
                    break;
                case LogMessageLevel.Information:
                    Logger.Info(message, exception);
                    break;
            }
        }
    }
}
