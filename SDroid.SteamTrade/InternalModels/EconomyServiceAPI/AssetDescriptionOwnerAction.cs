using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class AssetDescriptionOwnerAction
    {
        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}