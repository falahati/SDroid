using Newtonsoft.Json;
using SDroid.SteamTrade.InternalModels.EconomyServiceAPI.Constants;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class TradeOffer
    {
        [JsonProperty("accountid_other")]
        public int AccountIdOther { get; set; }

        [JsonProperty("confirmation_method")]
        public EconomyTradeOfferConfirmationMethod ConfirmationMethod { get; set; }

        [JsonProperty("escrow_end_date")]
        public int EscrowEndDate { get; set; }

        [JsonProperty("expiration_time")]
        public int ExpirationTime { get; set; }

        [JsonProperty("from_real_time_trade")]
        public bool FromRealTimeTrade { get; set; }

        [JsonProperty("is_our_offer")]
        public bool IsOurOffer { get; set; }

        [JsonProperty("items_to_give")]
        public Asset[] ItemsToGive { get; set; }

        [JsonProperty("items_to_receive")]
        public Asset[] ItemsToReceive { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("trade_offer_state")]
        public EconomyTradeOfferState State { get; set; }

        [JsonProperty("time_created")]
        public int TimeCreated { get; set; }

        [JsonProperty("time_updated")]
        public int TimeUpdated { get; set; }

        [JsonProperty("tradeofferid")]
        public long? TradeOfferId { get; set; }

        public bool IsValid()
        {
            return TradeOfferId > 0 &&
                   State != EconomyTradeOfferState.Unknown &&
                   State != EconomyTradeOfferState.Invalid &&
                   (ItemsToGive?.Length > 0 || ItemsToReceive?.Length > 0);
        }
    }
}