using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SDroid.SteamTrade.Helpers;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class TradeExchangeStatusAssetDescription
    {
        private const string ImageBaseAddress = "https://steamcommunity-a.akamaihd.net/economy/image/";

        [JsonProperty("appid")]
        public long AppId { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }

        [JsonProperty("classid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long ClassId { get; set; }

        [JsonProperty("descriptions")]
        public AssetDescriptionEntity[] Descriptions { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("icon_url_large")]
        public string IconUrlLarge { get; set; }

        [JsonProperty("instanceid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long InstanceId { get; set; }

        [JsonProperty("currency")]
        public bool IsCurrency { get; set; }

        [JsonProperty("tradable")]
        public bool IsTradable { get; set; }

        [JsonProperty("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonProperty("market_name")]
        public string MarketName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_color")]
        public string NameColor { get; set; }

        [JsonProperty("actions")]
        public List<AssetDescriptionOwnerAction> Actions { get; set; }

        [JsonProperty("market_actions")]
        public List<AssetDescriptionOwnerAction> MarketActions { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("commodity")]
        public bool IsCommodity { get; set; }

        [JsonProperty("market_tradable_restriction")]
        public int MarketTradableRestriction { get; set; }

        [JsonProperty("marketable")]
        public bool IsMarketable { get; set; }

        public UserInventoryAssetDescription ToSteamAssetDescription()
        {
            return new UserInventoryAssetDescription(
                AppId,
                ClassId,
                InstanceId,
                IconUrl = string.IsNullOrWhiteSpace(IconUrl) ? null : ImageBaseAddress + IconUrl,
                IconUrlLarge = string.IsNullOrWhiteSpace(IconUrlLarge) ? null : ImageBaseAddress + IconUrlLarge,
                Name,
                MarketHashName,
                MarketName,
                Type,
                IsTradable,
                IsMarketable,
                IsCommodity,
                IsCurrency,
                new UserInventoryAssetDescriptionEntry[0],
                new UserInventoryAssetDescriptionTag[0],
                Actions?.Select(action => action.ToUserInventoryAssetDescriptionAction()).ToArray() ?? new UserInventoryAssetDescriptionAction[0]
            );
        }
    }
}