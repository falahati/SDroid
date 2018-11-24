using Newtonsoft.Json;
using SDroid.SteamTrade.Helpers;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryResponseV2
    {
        [JsonProperty("assets")]
        public InventoryAssetV2[] Assets { get; set; }

        [JsonProperty("descriptions")]
        public InventoryAssetDescriptionV2[] Descriptions { get; set; }

        [JsonProperty("last_assetid")]
        [JsonConverter(typeof(JsonAsStringConverter<long?>))]
        public long? LastAssetId { get; set; }

        [JsonProperty("more_items")]
        [JsonConverter(typeof(JsonBoolAsIntConverter))]
        public bool MoreItems { get; set; }

        [JsonProperty("success")]
        [JsonConverter(typeof(JsonBoolAsIntConverter))]
        public bool Success { get; set; }

        [JsonProperty("total_inventory_count")]
        public int TotalInventoryCount { get; set; }
    }
}