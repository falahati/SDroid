using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SDroid.SteamTrade.InternalModels.EconomyItemsAPI.Constants;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class GetSchemaItemsResult
    {
        [JsonProperty("items")]
        public SchemaItem[] Items { get; set; }

        [JsonProperty("items_game_url")]
        public string ItemsGameUrl { get; set; }

        [JsonProperty("next")]
        public int Next { get; set; }

        [JsonProperty("status")]
        public GetSchemaStatus Status { get; set; }

        [JsonProperty("WebApiErrorMessage")]
        public string WebApiErrorMessage { get; set; }

        public SchemaItem GetItem(int itemDefinitionIndex)
        {
            return Items.FirstOrDefault(item => item.DefinitionIndex == itemDefinitionIndex);
        }

        public List<SchemaItem> GetItemsByCraftingMaterial(string material)
        {
            return Items.Where(item => item.CraftMaterialType == material).ToList();
        }
    }
}