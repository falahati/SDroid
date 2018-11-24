using Newtonsoft.Json;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryAppContext
    {
        [JsonProperty("asset_count")]
        public int Assets { get; set; }

        [JsonProperty("id")]
        public long ContextId { get; set; }

        [JsonProperty("name")]
        public string ContextName { get; set; }

        public UserInventoryAppContext ToUserInventoryAppContext()
        {
            return new UserInventoryAppContext(ContextId, ContextName);
        }
    }
}