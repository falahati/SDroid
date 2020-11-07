using Newtonsoft.Json;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryAssetDescriptionActionV2
    {
        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("name")]
        public string Title { get; set; }

        public UserInventoryAssetDescriptionAction ToUserInventoryAssetDescriptionAction()
        {
            return new UserInventoryAssetDescriptionAction(Title, Link);
        }
    }
}