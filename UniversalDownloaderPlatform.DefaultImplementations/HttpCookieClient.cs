using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace UniversalDownloaderPlatform.DefaultImplementations
{
    /// <summary>
    /// Wrapper around HttpClient with custom management of cookies
    /// Implemented because some users reported HttpClient losing cookies
    /// And I have not been able to find the reason why that was happening
    /// See issue #125 in PatreonDownloader GitHub repository
    /// </summary>
    internal class HttpCookieClient
    {
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public HttpCookieClient(string userAgent, CookieCollection preExistingCookies, IWebProxy proxy = null)
        {
            HttpClientHandler handler = new HttpClientHandler();
            if (proxy != null)
                handler.Proxy = proxy;

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);

            _cookieContainer = new CookieContainer();
            _cookieContainer.Add(preExistingCookies);
        }

        public void AddOrUpdateCookies(CookieCollection cookieCollection)
        {
            _cookieContainer.Add(cookieCollection);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            HttpCompletionOption completionOption)
        {
            if (request.RequestUri == null)
                throw new ArgumentException("Request should contain Uri");

            _logger.Trace($"New request to: {request.RequestUri}");

            List<string> cookieStringsList = new List<string>();
            foreach (Cookie cookie in _cookieContainer.GetCookies(request.RequestUri))
            {
                if (!cookie.Expired)
                    cookieStringsList.Add($"{cookie.Name}={cookie.Value}");
            }

            if (cookieStringsList.Count > 0)
            {
                string cookieString = string.Join("; ", cookieStringsList);
                _logger.Trace($"Cookies to be sent ({cookieStringsList.Count}): {cookieString}");
                request.Headers.Add("Cookie", string.Join("; ", cookieStringsList));
            }

            HttpResponseMessage response = await _httpClient.SendAsync(request, completionOption);

            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaderValues))
            {
                foreach (string setCookieHeaderValue in setCookieHeaderValues)
                {
                    _logger.Trace($"Response contains new/updated cookie: {setCookieHeaderValue}");

                    _cookieContainer.SetCookies(request.RequestUri, setCookieHeaderValue);
                }
            }

            return response;
        }
    }
}
