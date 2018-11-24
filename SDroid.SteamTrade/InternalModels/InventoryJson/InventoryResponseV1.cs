using System.Collections.Generic;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryResponseV1
    {
        [JsonProperty("rgAppInfo")]
        public InventoryAppInfoV1[] Apps { get; set; }

        [JsonProperty("rgInventory")]
        public Dictionary<string, InventoryAssetV1> Assets { get; set; }

        [JsonProperty("rgCurrency")]
        public Dictionary<string, InventoryAssetV1> Currencies { get; set; }

        [JsonProperty("rgDescriptions")]
        public Dictionary<string, InventoryAssetDescriptionV1> Descriptions { get; set; }

        [JsonProperty("more")]
        public bool More { get; set; }

        [JsonProperty("more_start")]
        public int MoreStart { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}