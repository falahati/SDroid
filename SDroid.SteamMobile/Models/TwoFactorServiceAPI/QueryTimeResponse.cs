using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.TwoFactorServiceAPI
{
    internal class QueryTimeResponse
    {
        [JsonProperty("server_time")]
        public long ServerTime { get; set; }
    }
}