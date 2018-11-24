using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.TwoFactorServiceAPI
{
    internal class FinalizeAuthenticatorResponse
    {
        [JsonProperty("server_time")]
        public ulong ServerTime { get; set; }

        [JsonProperty("status")]
        public AuthenticatorLinkerErrorCode Status { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("want_more")]
        public bool WantMore { get; set; }
    }
}