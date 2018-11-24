using System.Collections.Generic;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaItem
    {
        [JsonProperty("attributes")]
        public SchemaAttribute[] Attributes { get; set; }

        [JsonProperty("capabilities")]
        public SchemaItemCapabilities Capabilities { get; set; }

        [JsonProperty("craft_class")]
        public string CraftClass { get; set; }

        [JsonProperty("craft_material_type")]
        public string CraftMaterialType { get; set; }

        [JsonProperty("defindex")]
        public int DefinitionIndex { get; set; }

        [JsonProperty("drop_type")]
        public string DropType { get; set; }

        [JsonProperty("holiday_restriction")]
        public string HolidayRestriction { get; set; }

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty("image_url_large")]
        public string ImageUrlLarge { get; set; }

        [JsonProperty("image_inventory")]
        public string InventoryImage { get; set; }

        [JsonProperty("item_class")]
        public string ItemClass { get; set; }

        [JsonProperty("item_description")]
        public string ItemDescription { get; set; }

        [JsonProperty("item_name")]
        public string ItemName { get; set; }

        [JsonProperty("item_quality")]
        public int ItemQuality { get; set; }

        [JsonProperty("item_set")]
        public string ItemSet { get; set; }

        [JsonProperty("item_slot")]
        public string ItemSlot { get; set; }

        [JsonProperty("item_type_name")]
        public string ItemTypeName { get; set; }

        [JsonProperty("max_ilevel")]
        public string MaximumLevel { get; set; }

        [JsonProperty("min_ilevel")]
        public string MinimumLevel { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("per_class_loadout_slots")]
        public Dictionary<string, string> PerClassLoadOutSlots { get; set; }

        [JsonProperty("model_player")]
        public string PlayerModel { get; set; }

        [JsonProperty("proper_name")]
        public bool ProperName { get; set; }

        [JsonProperty("styles")]
        public SchemaItemStyle[] Styles { get; set; }

        [JsonProperty("tool")]
        public SchemaItemTool Tool { get; set; }

        [JsonProperty("used_by_classes")]
        public string[] UsableByClasses { get; set; }
    }
}