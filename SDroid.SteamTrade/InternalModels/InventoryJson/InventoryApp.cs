﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDroid.SteamTrade.Helpers;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryApp
    {
        [JsonProperty("appid")]
        public long AppId { get; set; }

        [JsonProperty("asset_count")]
        public int Assets { get; set; }

        [JsonProperty("rgContexts")]
        public JToken Contexts { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("inventory_logo")]
        public string InventoryLogo { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("load_failed")]
        [JsonConverter(typeof(JsonBoolAsIntConverter))]
        public bool LoadFailed { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("store_vetted")]
        [JsonConverter(typeof(JsonBoolAsIntConverter))]
        public bool StoreVetted { get; set; }

        [JsonProperty("trade_permissions")]
        public string TradePermissions { get; set; }

        public UserInventoryApp ToUserInventoryApp()
        {
            return new UserInventoryApp(
                AppId,
                Name,
                Icon,
                Contexts.Type == JTokenType.Object ? Contexts.ToObject<Dictionary<string, InventoryAppContext>>().Values.Select(c => c.ToUserInventoryAppContext()).ToArray() : new UserInventoryAppContext[0],
                InventoryLogo,
                StoreVetted,
                TradePermissions
            );
        }
    }
}