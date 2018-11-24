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
            QueryStringBuilder data) : this(url)
        {
            Method = method;
            Data = data;
        }

        public bool AcceptFailureResponses { get; set; }

        public QueryStringBuilder Data { get; set; }
        public NameValueCollection Headers { get; set; }
        public bool IsAjax { get; set; }
        public SteamWebAccessRequestMethod Method { get; set; } = SteamWebAccessRequestMethod.Get;
        public string Referer { get; set; }
        public int Timeout { get; set; } = 60000;
        public string Url { get; }

        public SteamWebAccessRequest Clone()
        {
            return new SteamWebAccessRequest(Url)
            {
                Data = Data != null ? new QueryStringBuilder(Data) : null,
                Headers = Headers != null ? new NameValueCollection(Headers) : null,
                Method = Method,
                Referer = Referer,
                IsAjax = IsAjax,
                AcceptFailureResponses = AcceptFailureResponses
            };
        }
    }
}