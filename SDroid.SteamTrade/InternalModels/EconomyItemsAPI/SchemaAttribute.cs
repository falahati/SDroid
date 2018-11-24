using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaAttribute
    {
        [JsonProperty("class")]
        public string AttributeClass { get; set; }

        [JsonProperty("name")]
        public string AttributeName { get; set; }

        [JsonProperty("value")]
        public string AttributeValue { get; set; }
    }
}