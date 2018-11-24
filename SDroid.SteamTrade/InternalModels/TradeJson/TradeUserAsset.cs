using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.TradeJson
{
    internal class TradeUserAsset
    {
        public TradeUserAsset(Asset asset)
        {
            AppId = asset.AppId;
            ContextId = asset.ContextId;
            AssetId = asset.AssetId;
            Amount = asset.Amount;
        }

        public TradeUserAsset()
        {
        }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("appid")]
        public long AppId { get; set; }

        [JsonProperty("assetid")]
        public long AssetId { get; set; }

        [JsonProperty("contextid")]
        public long ContextId { get; set; }

        public Asset ToAsset()
        {
            return new Asset(AppId, ContextId, AssetId, Amount);
        }
    }
}