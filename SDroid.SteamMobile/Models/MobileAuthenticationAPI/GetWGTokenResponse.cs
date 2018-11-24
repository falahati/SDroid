using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.MobileAuthenticationAPI
{
    internal class GetWGTokenResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("token_secure")]
        public string TokenSecure { get; set; }
    }
}