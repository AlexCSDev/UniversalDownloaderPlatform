using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using NLog;
using UniversalDownloaderPlatform.Common.Helpers;
using File = Google.Apis.Drive.v3.Data.File;

namespace UniversalDownloaderPlatform.GoogleDriveDownloader
{
    internal class GoogleDriveEngine
    {
        private static readonly Dictionary<string, string> _mimeTypeByExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Documents
            { ".TXT", "text/plain" },
            { ".RTF", "application/rtf" },
            { ".PDF", "application/pdf" },
            { ".DOCX", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".ODT", "application/vnd.oasis.opendocument.text" },

            // Spreadsheets
            { ".XLSX", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ODS", "application/x-vnd.oasis.opendocument.spreadsheet" },
            { ".CSV", "text/csv" }
        };

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
                    GoogleClientSecrets.Load(stream).Secrets,
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

        public void Download(string id, string path, bool overwrite = false)
        {
            if (Service == null)
                return;

            File fileResource = Service.Files.Get(id).Execute();

            DownloadFileResource(fileResource, path, true, overwrite);
        }

        private void DownloadFileResource(File fileResource, string path, bool rootPath = true, bool overwrite = false)
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

            Logger.Info($"[Google Drive] Downloading {fileResource.Name} '{fileResource.MimeType}'");

            if (fileResource.MimeType != "application/vnd.google-apps.folder")
            {
                // Handle no extension and google doc file
                string extension = Path.GetExtension(path);
                string mimeType;
                if (String.IsNullOrEmpty(extension))
                {
                    path += ".pdf";
                    mimeType = "application/pdf";
                }
                else
                {
                    _mimeTypeByExtensions.TryGetValue(extension, out mimeType);
                }

                if (System.IO.File.Exists(path))
                {
                    if (!overwrite)
                    {
                        Logger.Warn("[Google Drive] FILE EXISTS: " + path);
                        return;
                    }
                    else
                        System.IO.File.Delete(path);
                }

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(new FileInfo(path).DirectoryName);
                }

                using (System.IO.FileStream file = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
                {
                    // https://developers.google.com/drive/api/v3/mime-types
                    // https://developers.google.com/drive/api/v3/ref-export-formats
                    if (fileResource.MimeType == "application/vnd.google-apps.document" ||
                        fileResource.MimeType == "application/vnd.google-apps.spreadsheet")
                    {
                        Service.Files.Export(fileResource.Id, mimeType).Download(file);
                    }
                    else
                    {
                        Service.Files.Get(fileResource.Id).Download(file);
                    }
                }
            }
            else
            {

                System.IO.Directory.CreateDirectory(path);
                var subFolderItems = RessInFolder(fileResource.Id);

                foreach (var item in subFolderItems)
                    DownloadFileResource(item, path, false);
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
            } while (!String.IsNullOrEmpty(request.PageToken));

            Logger.Info($"[Google Drive] Finished scanning folder {folderId}");
            return retList;
        }
    }
}
