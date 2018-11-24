using Newtonsoft.Json;
using SDroid.SteamTrade.Helpers;

namespace SDroid.SteamTrade.InternalModels.TradeOfferJson
{
    internal class TradeOfferAsset
    {
        [JsonProperty("amount")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long Amount { get; set; }

        [JsonProperty("appid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long AppId { get; set; }

        [JsonProperty("assetid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long AssetId { get; set; }

        [JsonProperty("contextid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long ContextId { get; set; }

        [JsonProperty("currencyid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long CurrencyId { get; set; }

        public static TradeOfferAsset FromAsset(Asset asset)
        {
            return new TradeOfferAsset
            {
                AppId = asset.AppId,
                ContextId = asset.ContextId,
                AssetId = asset.AssetId,
                Amount = asset.Amount,
                CurrencyId = 0
            };
        }
    }
}