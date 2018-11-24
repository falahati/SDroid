using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaOverviewItemLevelDetail
    {
        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("required_score")]
        public long RequiredScore { get; set; }
    }
}