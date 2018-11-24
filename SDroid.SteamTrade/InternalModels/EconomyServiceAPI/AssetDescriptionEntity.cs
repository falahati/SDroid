using System.Collections.Generic;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class AssetDescriptionEntity
    {
        [JsonProperty("app_data")]
        public Dictionary<string, string> AppData { get; set; }

        [JsonProperty("color")]
        public string Color { get; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}