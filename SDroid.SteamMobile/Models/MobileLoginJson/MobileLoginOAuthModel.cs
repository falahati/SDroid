using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.MobileLoginJson
{
    internal class MobileLoginOAuthModel
    {
        [JsonProperty("account_name")]
        public string AccountName { get; set; }

        [JsonProperty("oauth_token")]
        public string OAuthToken { get; set; }

        [JsonProperty("steamid")]
        public ulong SteamId { get; set; }

        [JsonProperty("wgtoken")]
        public string Token { get; set; }

        [JsonProperty("wgtoken_secure")]
        public string TokenSecure { get; set; }

        [JsonProperty("webcookie")]
        public string Webcookie { get; set; }
    }
}