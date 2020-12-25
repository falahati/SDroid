using Newtonsoft.Json;
using SDroid.SteamTrade.Helpers;
using SDroid.SteamTrade.Models.Trade;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class TradeExchangeStatusAsset
    {
        [JsonProperty("amount")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long Amount { get; set; }

        [JsonProperty("appid")]
        public long AppId { get; set; }

        [JsonProperty("assetid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long AssetId { get; set; }

        [JsonProperty("new_assetid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long NewAssetId { get; set; }

        [JsonProperty("classid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long ClassId { get; set; }

        [JsonProperty("contextid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long ContextId { get; set; }

        [JsonProperty("new_contextid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long NewContextId { get; set; }

        [JsonProperty("instanceid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long InstanceId { get; set; }

        public TradeExchangeAsset ToTradeExchangeAsset()
        {
            return new TradeExchangeAsset(
                AppId,
                ContextId,
                AssetId,
                ClassId,
                InstanceId,
                NewContextId,
                NewAssetId,
                Amount
            );
        }
    }
}