using Newtonsoft.Json;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class AssetDescriptionOwnerAction
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