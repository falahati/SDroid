using Newtonsoft.Json;

namespace SDroid.InternalModels.SteamUserAPI
{
    internal class ResolveVanityUrlResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("steamid")]
        public ulong SteamId { get; set; }

        [JsonProperty("success")]
        public ResolveVanityUrlResponseStatus Success { get; set; }
    }
}