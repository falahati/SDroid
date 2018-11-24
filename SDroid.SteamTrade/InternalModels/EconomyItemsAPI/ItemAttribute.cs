using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class ItemAttribute
    {
        [JsonProperty("account_info")]
        public ItemAttributeAccountInfo AccountInfo { get; set; }

        [JsonProperty("value")]
        public string AttributeValue { get; set; }

        [JsonProperty("defindex")]
        public int DefinitionIndex { get; set; }

        [JsonProperty("float_value")]
        public decimal? NumericValue { get; set; }
    }
}