using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.MobileLoginJson
{
    internal class MobileLoginResponse
    {
        [JsonIgnore]
        public MobileLoginOAuthModel OAuthToken
        {
            get => !string.IsNullOrWhiteSpace(OAuthTokenString)
                ? JsonConvert.DeserializeObject<MobileLoginOAuthModel>(OAuthTokenString)
                : null;
        }

        [JsonProperty("oauth")]
        public string OAuthTokenString { get; set; }
    }
}