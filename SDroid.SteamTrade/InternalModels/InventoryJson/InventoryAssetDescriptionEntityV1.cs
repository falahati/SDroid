using System.Collections.Generic;
using Newtonsoft.Json;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryAssetDescriptionEntityV1
    {
        [JsonProperty("app_data")]
        public Dictionary<string, string> AppData { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        public UserInventoryAssetDescriptionEntry ToSteamAssetDescriptionEntry()
        {
            return new UserInventoryAssetDescriptionEntry(Type, Value, AppData);
        }
    }
}