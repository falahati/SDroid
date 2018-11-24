using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.TradeOfferJson
{
    internal class TradeOfferCreateParameters
    {
        [JsonProperty("trade_offer_access_token")]
        public string TradeOfferAccessToken { get; set; }
    }
}