using Newtonsoft.Json;

namespace SDroid.SteamMobile.InternalModels
{
    internal class PollAuthSessionStatusResponse
    {
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("had_remote_interaction")]
        public bool HadRemoteIntraction { get; set; }
        
        [JsonProperty("account_nam")]
        public string AccountName { get; set; }
    }
}
