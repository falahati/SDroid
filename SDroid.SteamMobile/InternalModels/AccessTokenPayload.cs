using Newtonsoft.Json;

namespace SDroid.SteamMobile.InternalModels
{
    internal class AccessTokenPayload
    {
        [JsonProperty("exp")]
        public long Expiry { get; set; }
    }
}