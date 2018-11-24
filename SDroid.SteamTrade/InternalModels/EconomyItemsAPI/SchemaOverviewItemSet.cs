using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaOverviewItemSet
    {
        [JsonProperty("attributes")]
        public SchemaAttribute[] Attributes { get; set; }

        [JsonProperty("items")]
        public string[] Items { get; set; }

        [JsonProperty("item_set")]
        public string ItemSet { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("store_bundle")]
        public string StoreBundleName { get; set; }
    }
}