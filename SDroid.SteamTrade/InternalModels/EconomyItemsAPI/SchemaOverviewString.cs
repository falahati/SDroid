using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaOverviewString
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("string")]
        public string Value { get; set; }
    }
}