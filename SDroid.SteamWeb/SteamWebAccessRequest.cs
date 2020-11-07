using System.Collections.Specialized;

namespace SDroid.SteamWeb
{
    public class SteamWebAccessRequest
    {
        public SteamWebAccessRequest(string url)
        {
            Url = url;
        }

        public SteamWebAccessRequest(
            string url,
            SteamWebAccessRequestMethod method,
            QueryStringBuilder formData) : this(url)
        {
            Method = method;
            FormData = formData;
        }

        public bool AcceptFailureResponses { get; set; }

        public QueryStringBuilder FormData { get; set; }
        public NameValueCollection Headers { get; set; }
        public bool IsUpload { get; set; } = false;
        public bool IsAjax { get; set; }
        public SteamWebAccessRequestMethod Method { get; set; } = SteamWebAccessRequestMethod.Get;
        public string Referer { get; set; }
        public int Timeout { get; set; } = 60000;
        public string Url { get; }

        public SteamWebAccessRequest Clone()
        {
            return new SteamWebAccessRequest(Url)
            {
                FormData = FormData != null ? new QueryStringBuilder(FormData) : null,
                Headers = Headers != null ? new NameValueCollection(Headers) : null,
                Method = Method,
                Referer = Referer,
                IsAjax = IsAjax,
                AcceptFailureResponses = AcceptFailureResponses
            };
        }
    }
}