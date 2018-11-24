using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaItemTool
    {
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}