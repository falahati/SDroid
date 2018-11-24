using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.TradeOfferJson
{
    internal class TradeOfferCreateResponse
    {
        [JsonProperty("strError")]
        public string TradeError { get; set; }

        [JsonProperty("tradeofferid")]
        public long? TradeOfferId { get; set; }
    }
}