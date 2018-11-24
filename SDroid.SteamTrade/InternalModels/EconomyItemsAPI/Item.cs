using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class Item
    {
        [JsonProperty("id")]
        public long AssetId { get; set; }

        [JsonProperty("attributes")]
        public ItemAttribute[] Attributes { get; set; }

        [JsonProperty("defindex")]
        public int DefinitionIndex { get; set; }

        [JsonProperty("equipped")]
        public ItemEquippedSlot[] EquippedSlot { get; set; }

        [JsonProperty("flag_cannot_craft")]
        public bool IsNotCraftable { get; set; }

        [JsonProperty("flag_cannot_trade")]
        public bool IsNotTradable { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("origin")]
        public int Origin { get; set; }

        [JsonProperty("original_id")]
        public long OriginalAssetId { get; set; }

        [JsonProperty("quality")]
        public int? Quality { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("style")]
        public int Style { get; set; }
    }
}