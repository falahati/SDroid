using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryAssetDescriptionActionV2
    {
        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}