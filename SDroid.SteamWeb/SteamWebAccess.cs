using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SDroid.SteamWeb
{
    public class SteamWebAccess
    {
        public const string CommunityBaseUrl = "https://steamcommunity.com";
        public const string SteamLanguage = "english";

        protected const string UserAgent = "Dalvik/2.1.0 (Linux; U; Android 9; Valve Steam App Version/3)";

        private IPAddress _ipAddress;
        private IWebProxy _proxy;
        private WebSession _session;

        public SteamWebAccess(WebSession session)
        {
            _session = session;
            _ipAddress = IPAddress.Any;
            _proxy = null;
        }

        public SteamWebAccess(WebSession session, IPAddress ipAddress) : this(session)
        {
            _ipAddress = ipAddress;
        }

        public SteamWebAccess(WebSession session, IWebProxy proxy) : this(session)
        {
            _proxy = proxy;
        }

        public SteamWebAccess(WebSession session, IPAddress ipAddress, IWebProxy proxy) : this(session)
        {
            _ipAddress = ipAddress;
            _proxy = proxy;
        }

        public virtual IPAddress IPAddress
        {
            get => _ipAddress;
            set => _ipAddress = value;
        }

        public virtual IWebProxy Proxy
        {
            get => _proxy;
            set => _proxy = value;
        }

        public virtual WebSession Session
        {
            get => _session;
            set => _session = value ?? throw new ArgumentNullException(nameof(value));
        }

        public static SteamWebAccess GetGuest()
        {
            return new SteamWebAccess(new WebSession());
        }

        public static SteamWebAccess GetGuest(IPAddress ipAddress)
        {
            return new SteamWebAccess(new WebSession(), ipAddress);
        }

        public static SteamWebAccess GetGuest(IWebProxy proxy)
        {
            return new SteamWebAccess(new WebSession(), proxy);
        }

        /// <summary>
        ///     Sends a request to a remote HTTP server and returns the response as a Stream
        /// </summary>
        /// <returns>Response of the HTTP server. Should be disposed after reading.</returns>
        /// <exception cref="WebException">
        ///     Unexpected status code. - null
        ///     or
        ///     Empty response returned. - null
        ///     or
        ///     Server returned a bad response. - null
        /// </exception>
        public virtual async Task<MemoryStream> FetchBinary(SteamWebAccessRequest accessRequest)
        {
            HttpWebResponse webResponse = null;
            HttpWebRequest webRequest = null;

            try
            {
                var url = accessRequest.Url;
                byte[] postData = null;
                if (accessRequest.FormData != null &&
                    accessRequest.FormData.Count > 0)
                {
                    if (accessRequest.Method == SteamWebAccessRequestMethod.Post)
                    {
                        if (accessRequest.IsUpload)
                        {
                            postData = await accessRequest.FormData.ToMultipartFormDataContent().ReadAsByteArrayAsync();
                        }
                        else
                        {
                            postData = Encoding.UTF8.GetBytes(accessRequest.FormData.ToString());
                        }
                    }
                    else
                    {
                        url = accessRequest.FormData.AppendToUrl(url);
                    }
                }

                webRequest = await MakeRequest(url, postData, accessRequest).ConfigureAwait(false);

                if (webRequest == null)
                {
                    return new MemoryStream();
                }

                await WriteRequest(
                    webRequest,
                    postData,
                    accessRequest
                ).ConfigureAwait(false);

                webResponse = (HttpWebResponse) await webRequest.GetResponseAsync().ConfigureAwait(false);
            }
            catch (WebException e)
            {
                if (accessRequest.AcceptFailureResponses)
                {
                    webResponse = e.Response as HttpWebResponse;
                }

                if (webResponse == null)
                {
                    throw;
                }
            }

            return await ReadResponse(webRequest, webResponse, accessRequest).ConfigureAwait(false);
        }

        /// <summary>
        ///     Sends a request to a remote HTTP server and returns the JSON response de-serialized as an object
        /// </summary>
        /// <returns>Response of the HTTP server.</returns>
        /// <exception cref="WebException">
        ///     Unexpected status code. - null
        ///     or
        ///     Empty response returned. - null
        ///     or
        ///     Server returned a bad response. - null
        /// </exception>
        public virtual async Task<dynamic> FetchDynamic(SteamWebAccessRequest webAccessRequest)
        {
            var json = await FetchString(webAccessRequest).ConfigureAwait(false);

            return JsonConvert.DeserializeObject(json);
        }

        /// <summary>
        ///     Sends a request to a remote HTTP server and returns the JSON response de-serialized as an object
        /// </summary>
        /// <returns>Response of the HTTP server.</returns>
        /// <exception cref="WebException">
        ///     Unexpected status code. - null
        ///     or
        ///     Empty response returned. - null
        ///     or
        ///     Server returned a bad response. - null
        /// </exception>
        public virtual async Task<T> FetchObject<T>(SteamWebAccessRequest webAccessRequest)
        {
            var json = await FetchString(webAccessRequest).ConfigureAwait(false);

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException)
            {
                return default(T);
            }
        }

        /// <summary>
        ///     Sends a request to a remote HTTP server and returns the response as a string
        /// </summary>
        /// <returns>Response of the HTTP server.</returns>
        /// <exception cref="WebException">
        ///     Unexpected status code. - null
        ///     or
        ///     Empty response returned. - null
        ///     or
        ///     Server returned a bad response. - null
        /// </exception>
        public virtual async Task<string> FetchString(SteamWebAccessRequest webAccessRequest)
        {
            var responseStream = await FetchBinary(webAccessRequest).ConfigureAwait(false);
            responseStream.Seek(0, SeekOrigin.Begin);

            using (var responseReader = new StreamReader(responseStream))
            {
                return await responseReader.ReadToEndAsync().ConfigureAwait(false);
            }
        }


        public async Task<IPAddress> GetActualIPAddress()
        {
            try
            {
                var result = await FetchString(
                    new SteamWebAccessRequest(
                        @"https://ipinfo.io/json",
                        SteamWebAccessRequestMethod.Get,
                        null
                    )
                ).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    var ip = JsonConvert.DeserializeAnonymousType(result, new {ip = ""});

                    return ip != null ? IPAddress.Parse(ip.ip) : null;
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        //public virtual async Task<bool> VerifySession()
        //{
        //    if (string.IsNullOrWhiteSpace(Session?.SteamLogin) &&
        //        string.IsNullOrWhiteSpace(Session?.SteamLoginSecure))
        //    {
        //        return false;
        //    }

        //    try
        //    {
        //        (await FetchBinary(new SteamWebAccessRequest(CommunityBaseUrl)).ConfigureAwait(false)).Dispose();
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    return !string.IsNullOrWhiteSpace(Session?.SteamLogin) ||
        //           !string.IsNullOrWhiteSpace(Session?.SteamLoginSecure);
        //}

        protected virtual Task<HttpWebRequest> MakeRequest(
            string url,
            byte[] body,
            SteamWebAccessRequest accessRequest
        )
        {
            var webRequest = (HttpWebRequest) WebRequest.Create(url);
            webRequest.Method = accessRequest.Method == SteamWebAccessRequestMethod.Post ? "POST" : "GET";
            webRequest.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
            webRequest.UserAgent = UserAgent;
            webRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            webRequest.Referer = accessRequest.Referer ?? CommunityBaseUrl;
            webRequest.Timeout = accessRequest.Timeout;
            webRequest.KeepAlive = false;
            webRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Revalidate);
            webRequest.Headers[HttpRequestHeader.AcceptLanguage] = "en, en-us;q=0.9, en-gb;q=0.8";
            if (
                IPAddress != null &&
                !IPAddress.Equals(IPAddress.Any) &&
                IPAddress.Equals(IPAddress.IPv6Any) &&
                !IPAddress.IsLoopback(IPAddress)
            )
            {
                webRequest.ServicePoint.BindIPEndPointDelegate =
                    (point, endPoint, count) => new IPEndPoint(IPAddress, 0);
            }

            if (Proxy != null)
            {
                webRequest.Proxy = Proxy;
            }

            if (accessRequest.Headers != null)
            {
                webRequest.Headers.Add(accessRequest.Headers);
            }

            if (accessRequest.IsAjax)
            {
                webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
                webRequest.Headers.Add("X-Prototype-Version", "1.7");
            }

            if (Session != null)
            {
                webRequest.CookieContainer = Session;
            }

            if (body != null)
            {
                webRequest.ContentLength = body.Length;
                webRequest.ContentType = accessRequest.IsUpload ? "multipart/form-data" : "application/x-www-form-urlencoded";
            }

            return Task.FromResult(webRequest);
        }

        protected virtual async Task<MemoryStream> ReadResponse(
            HttpWebRequest webRequest,
            HttpWebResponse webResponse,
            SteamWebAccessRequest accessRequest)
        {
            using (webResponse)
            {
                if (!accessRequest.AcceptFailureResponses && webResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new WebException("Unexpected status code.", null, WebExceptionStatus.UnknownError,
                        webResponse);
                }

                foreach (Cookie cookie in webResponse.Cookies)
                {
                    var found = false;

                    foreach (var c in webRequest?.CookieContainer?.GetCookies(webResponse.ResponseUri).Cast<Cookie>() ?? Array.Empty<Cookie>())
                    {
                        if (c.Name != cookie.Name)
                        {
                            continue;
                        }

                        c.Value = cookie.Value;
                        c.Expires = cookie.Expires;
                        c.Expired = cookie.Expired;
                        found = true;

                        break;
                    }

                    if (!found)
                    {
                        webRequest?.CookieContainer?.Add(cookie);
                    }
                }

                var responseStream = webResponse.GetResponseStream();
                if (responseStream == null)
                {
                    if (!accessRequest.AcceptFailureResponses)
                    {
                        throw new WebException("Empty response returned.", null, WebExceptionStatus.UnknownError,
                            webResponse);
                    }

                    return new MemoryStream();
                }

                using (responseStream)
                {
                    var memoryStream = new MemoryStream();
                    await responseStream.CopyToAsync(memoryStream, 16 * 1024).ConfigureAwait(false);

                    return memoryStream;
                }
            }
        }

        protected virtual async Task WriteRequest(
            HttpWebRequest webRequest,
            byte[] body,
            SteamWebAccessRequest accessRequest)
        {
            if (body != null)
            {
                using (var requestStream = await webRequest.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    await requestStream.WriteAsync(body, 0, body.Length);
                    requestStream.Close();
                }
            }
        }
    }
}