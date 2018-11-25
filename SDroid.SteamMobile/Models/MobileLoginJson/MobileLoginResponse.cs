using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.MobileLoginJson
{
    internal class MobileLoginResponse
    {
        [JsonProperty("login_complete")]
        public string LoginComplete { get; set; }

        [JsonIgnore]
        public MobileLoginOAuthModel OAuthToken
        {
            get => !string.IsNullOrWhiteSpace(OAuthTokenString)
                ? JsonConvert.DeserializeObject<MobileLoginOAuthModel>(OAuthTokenString)
                : null;
        }

        [JsonProperty("oauth")]
        public string OAuthTokenString { get; set; }

        [JsonProperty("redirect_uri")]
        public string RedirectUri { get; set; }

        [JsonProperty("success")]
        public string Success { get; set; }
    }
}