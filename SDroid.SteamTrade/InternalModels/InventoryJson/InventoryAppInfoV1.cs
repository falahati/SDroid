using Newtonsoft.Json;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryAppInfoV1
    {
        [JsonProperty("appid")]
        public long AppId { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public UserInventoryApp ToSteamInventoryAsset()
        {
            return new UserInventoryApp(AppId, Name, Icon);
        }
    }
}