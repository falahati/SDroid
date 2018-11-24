using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.TradeOfferJson
{
    internal class TradeOfferCancelResponse
    {
        [JsonProperty("strError")]
        public string Error { get; set; }

        [JsonProperty("tradeofferid")]
        public long? TradeOfferId { get; set; }
    }
}