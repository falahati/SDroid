using Newtonsoft.Json;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryAssetDescriptionTagV1
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("category_name")]
        public string LocalizedCategoryName { get; set; }

        [JsonProperty("name")]
        public string LocalizedName { get; set; }

        [JsonProperty("internal_name")]
        public string Name { get; set; }

        public UserInventoryAssetDescriptionTag ToSteamAssetDescriptionTag()
        {
            return new UserInventoryAssetDescriptionTag(Name, LocalizedName, Category, LocalizedCategoryName);
        }
    }
}