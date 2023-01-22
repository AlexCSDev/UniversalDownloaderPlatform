using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web;
using HeyRed.Mime;
using NLog;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Helpers;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    public class RemoteFileInfoRetriever : IRemoteFileInfoRetriever
    {
        private HttpClient _httpClient;

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private int _maxRetries;
        private int _retryMultiplier;
        private readonly Version _httpVersion = HttpVersion.Version20;

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _maxRetries = settings.MaxDownloadRetries;
            _retryMultiplier = settings.RetryMultiplier;

            HttpClientHandler httpClientHandler = new HttpClientHandler();
            if (settings.CookieContainer != null)
            {
                httpClientHandler.UseCookies = true;
                httpClientHandler.CookieContainer = settings.CookieContainer;
            }

            _httpClient = new HttpClient(httpClientHandler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        }

        public virtual async Task<(string, long)> GetRemoteFileInfo(string url, bool useMediaType, string refererUrl = null)
        {
            return await GetRemoteFileInfoInternal(url, useMediaType, refererUrl);
        }

        private async Task<(string, long)> GetRemoteFileInfoInternal(string url, bool useMediaType, string refererUrl, int retry = 0, int retryTooManyRequests = 0)
        {
            if (retry > 0)
            {
                if (retry >= _maxRetries)
                {
                    throw new Exception("Retries limit reached");
                }

                await Task.Delay(retry * _retryMultiplier * 1000);
            }

            if (retryTooManyRequests > 0)
                await Task.Delay(retryTooManyRequests * _retryMultiplier * 1000);

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, url) { Version = _httpVersion })
                {
                    if (!string.IsNullOrWhiteSpace(refererUrl))
                    {
                        try
                        {
                            request.Headers.Referrer = new Uri(refererUrl);
                        }
                        catch (UriFormatException ex)
                        {
                            _logger.Error(ex, $"[Remote file info] Invalid referer url: {refererUrl}. Error: {ex}");
                        }
                    }

                    using (HttpResponseMessage responseMessage =
                        await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            switch (responseMessage.StatusCode)
                            {
                                case HttpStatusCode.BadRequest:
                                case HttpStatusCode.Unauthorized:
                                case HttpStatusCode.Forbidden:
                                case HttpStatusCode.NotFound:
                                case HttpStatusCode.MethodNotAllowed:
                                case HttpStatusCode.Gone:
                                    throw new WebException(
                                        $"[Remote file info] Unable to get remote file size as status code is {responseMessage.StatusCode}");
                                case HttpStatusCode.Moved:
                                    string newLocation = responseMessage.Headers.Location.ToString();
                                    _logger.Debug(
                                        $"[Remote file info] {url} has been moved to: {newLocation}, retrying using new url");
                                    return await GetRemoteFileInfoInternal(newLocation, useMediaType, refererUrl);
                                case HttpStatusCode.TooManyRequests:
                                    retryTooManyRequests++;
                                    _logger.Debug($"[Remote file info] Too many requests for {url}, waiting for {retryTooManyRequests * _retryMultiplier} seconds...");
                                    return await GetRemoteFileInfoInternal(url, useMediaType, refererUrl, 0, retryTooManyRequests);
                            }

                            retry++;

                            _logger.Debug(
                                $"Remote file size check: {url} returned status code {responseMessage.StatusCode}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)...");
                            return await GetRemoteFileInfoInternal(url, useMediaType, refererUrl, retry);
                        }

                        string mediaType = null;
                        string filename = null;

                        //Some webservers (I'm looking at you discord) are stupid and returning url encoded values in headers (like %20 instead of space character)
                        //so let's parse this manually because .NET built-in functionality can't handle that
                        if (responseMessage.Content.Headers.TryGetValues("Content-Disposition", out IEnumerable<string> values))
                        {
                            string value = HttpUtility.UrlDecode(values.First());
                            _logger.Debug($"Content-Disposition value: {value}");
                            if(ContentDispositionHeaderValue.TryParse(value, out ContentDispositionHeaderValue parsedHeader) &&
                                !string.IsNullOrWhiteSpace(parsedHeader.FileName))
                            {
                                filename = parsedHeader.FileName.Replace("\"", "");
                                _logger.Debug($"Content-Disposition parsed and returned: {filename}");
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(responseMessage.Content.Headers.ContentType?.MediaType) && useMediaType)
                        {
                            mediaType = responseMessage.Content.Headers.ContentType?.MediaType;
                        }

                        else if (!string.IsNullOrWhiteSpace(mediaType))
                        {
                            filename =
                                $"gen_{HashHelper.ComputeSha256Hash(url).Substring(0, 20)}.{MimeTypesMap.GetExtension(mediaType)}";

                            _logger.Debug($"Content-Disposition and url extraction failed, fallback to Content-Type + hash based name: {filename}");
                        }

                        return (filename, responseMessage.Content.Headers.ContentLength ?? 0);
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                retry++;
                _logger.Debug(ex, $"Encountered error while trying to download {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileInfoInternal(url, useMediaType, refererUrl, retry);
            }
            catch (IOException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered IO error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileInfoInternal(url, useMediaType, refererUrl, retry);
            }
            catch (SocketException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered connection error while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileInfoInternal(url, useMediaType, refererUrl, retry);
            }
            catch (HttpRequestException ex)
            {
                retry++;
                _logger.Debug(ex,
                    $"Encountered http request exception while trying to access {url}, retrying in {retry * _retryMultiplier} seconds ({_maxRetries - retry} retries left)... The error is: {ex}");
                return await GetRemoteFileInfoInternal(url, useMediaType, refererUrl, retry);
            }
            catch (Exception ex)
            {
                throw new WebException($"Unable to download from {url}: {ex.Message}", ex);
            }
        }
    }
}