using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaOverviewStringLockup
    {
        [JsonProperty("strings")]
        public SchemaOverviewString[] Strings { get; set; }

        [JsonProperty("table_name")]
        public string TableName { get; set; }
    }
}