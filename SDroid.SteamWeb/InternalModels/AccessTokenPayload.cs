using Newtonsoft.Json;

namespace SDroid.SteamWeb.InternalModels
{
    internal class AccessTokenPayload
    {
        [JsonProperty("exp")]
        public long Expiry { get; set; }
    }
}