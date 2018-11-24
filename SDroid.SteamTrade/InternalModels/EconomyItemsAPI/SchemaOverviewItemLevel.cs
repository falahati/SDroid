using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaOverviewItemLevel
    {
        [JsonProperty("levels")]
        public SchemaOverviewItemLevelDetail[] Levels { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}