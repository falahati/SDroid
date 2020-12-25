using Newtonsoft.Json;
using SDroid.SteamTrade.Helpers;
using SDroid.SteamTrade.InternalModels.EconomyServiceAPI.Constants;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class TradeExchangeStatus
    {
        [JsonProperty("tradeid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long TradeId { get; set; }

        [JsonProperty("steamid_other")]
        [JsonConverter(typeof(JsonAsStringConverter<ulong>))]
        public ulong PartnerSteamId { get; set; }

        [JsonProperty("status")]
        public EconomyTradeOfferState State { get; set; }

        [JsonProperty("time_init")]
        public int TimeCreated { get; set; }

        [JsonProperty("assets_given")]
        public TradeExchangeStatusAsset[] ItemsGiven { get; set; }

        [JsonProperty("assets_received")]
        public TradeExchangeStatusAsset[] ItemsReceived { get; set; }

        public bool IsValid()
        {
            return TradeId > 0 &&
                   State != EconomyTradeOfferState.Unknown &&
                   State != EconomyTradeOfferState.Invalid &&
                   (ItemsReceived?.Length > 0 || ItemsGiven?.Length > 0);
        }
    }
}