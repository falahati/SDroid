using Newtonsoft.Json;

namespace SDroid.SteamWeb.Models
{
    public class LoginResponseTransferParameters
    {
        [JsonProperty("steamid")]
        public ulong SteamId { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("token_secure")]
        public string TokenSecure { get; set; }

        [JsonProperty("auth")]
        public string AuthenticationToken { get; set; }

        [JsonProperty("remember_login")]
        public bool? RememberLogin { get; set; }

        [JsonProperty("webcookie")]
        public string WebCookie { get; set; }

    }
}