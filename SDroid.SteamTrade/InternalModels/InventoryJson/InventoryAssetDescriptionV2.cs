using System.Linq;
using Newtonsoft.Json;
using SDroid.SteamTrade.Helpers;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryAssetDescriptionV2
    {
        private const string ImageBaseAddress = "https://steamcommunity-a.akamaihd.net/economy/image/";

        [JsonProperty("actions")]
        public InventoryAssetDescriptionActionV2[] Actions { get; set; }

        [JsonProperty("appid")]
        public long AppId { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }

        [JsonProperty("classid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long ClassId { get; set; }

        [JsonProperty("descriptions")]
        public InventoryAssetDescriptionEntityV2[] Descriptions { get; set; }

        [JsonProperty("owner_descriptions")]
        public InventoryAssetDescriptionEntityV2[] OwnerDescriptions { get; set; }

        [JsonProperty("icon_url")]
        public string IconUrl { get; set; }

        [JsonProperty("icon_url_large")]
        public string IconUrlLarge { get; set; }

        [JsonProperty("instanceid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long InstanceId { get; set; }

        [JsonProperty("commodity")]
        [JsonConverter(typeof(JsonBoolAsIntConverter))]
        public bool IsCommodity { get; set; }

        [JsonProperty("currency")]
        [JsonConverter(typeof(JsonBoolAsIntConverter))]
        public bool IsCurrency { get; }

        [JsonProperty("marketable")]
        [JsonConverter(typeof(JsonBoolAsIntConverter))]
        public bool IsMarketable { get; set; }

        [JsonProperty("tradable")]
        [JsonConverter(typeof(JsonBoolAsIntConverter))]
        public bool IsTradable { get; set; }

        [JsonProperty("market_fee_app")]
        public long MarketFeeApp { get; set; }

        [JsonProperty("market_hash_name")]
        public string MarketHashName { get; set; }

        [JsonProperty("market_marketable_restriction")]
        [JsonConverter(typeof(JsonAsStringConverter<int>))]
        public int MarketableRestriction { get; set; }

        [JsonProperty("market_name")]
        public string MarketName { get; set; }

        [JsonProperty("market_tradable_restriction")]
        [JsonConverter(typeof(JsonAsStringConverter<int>))]
        public int TradableRestriction { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tags")]
        public InventoryAssetDescriptionTagV2[] Tags { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        public UserInventoryAssetDescription ToSteamAssetDescription()
        {
            return new UserInventoryAssetDescription(
                AppId,
                ClassId,
                InstanceId,
                string.IsNullOrWhiteSpace(IconUrl) ? null : ImageBaseAddress + IconUrl,
                string.IsNullOrWhiteSpace(IconUrlLarge) ? null : ImageBaseAddress + IconUrlLarge,
                Name,
                MarketHashName,
                MarketName,
                Type,
                IsTradable,
                IsMarketable,
                IsCommodity,
                IsCurrency,
                TradableRestriction,
                MarketableRestriction,
                (Descriptions?.Select(entity => entity.ToSteamAssetDescriptionEntry()).ToArray() ?? new UserInventoryAssetDescriptionEntry[0]).Concat(
                    OwnerDescriptions?.Select(entity => entity.ToSteamAssetDescriptionEntry()).ToArray() ?? new UserInventoryAssetDescriptionEntry[0]
                ).ToArray(),
                Tags?.Select(tag => tag.ToSteamAssetDescriptionTag()).ToArray() ?? new UserInventoryAssetDescriptionTag[0],
                Actions?.Select(action => action.ToUserInventoryAssetDescriptionAction()).ToArray() ?? new UserInventoryAssetDescriptionAction[0]
            );
        }
    }
}