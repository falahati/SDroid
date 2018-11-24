using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaOverviewOriginName
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("origin")]
        public int Origin { get; set; }
    }
}