using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.MobileAuthenticationAPI
{
    internal class GenerateAccessTokenForAppResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}