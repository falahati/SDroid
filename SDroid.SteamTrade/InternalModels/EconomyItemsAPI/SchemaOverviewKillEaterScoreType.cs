using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaOverviewKillEaterScoreType
    {
        [JsonProperty("level_data")]
        public string LevelData { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("type_name")]
        public string TypeName { get; set; }
    }
}