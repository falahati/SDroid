using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaOverviewAttribute
    {
        [JsonProperty("attribute_class")]
        public string AttributeClass { get; set; }

        [JsonProperty("name")]
        public string AttributeName { get; set; }

        [JsonProperty("defindex")]
        public int DefinitionIndex { get; set; }

        [JsonProperty("description_format")]
        public string DescriptionFormat { get; set; }

        [JsonProperty("description_string")]
        public string DescriptionString { get; set; }

        [JsonProperty("effect_type")]
        public string EffectType { get; set; }

        [JsonProperty("hidden")]
        public bool IsHidden { get; set; }

        [JsonProperty("stored_as_integer")]
        public bool IsStoredAsInteger { get; set; }
    }
}