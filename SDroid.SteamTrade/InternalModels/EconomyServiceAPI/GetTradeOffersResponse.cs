using System.Linq;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class GetTradeOffersResponse
    {
        [JsonIgnore]
        public TradeOffer[] AllOffers
        {
            get => (TradeOffersSent ?? new TradeOffer[0]).Concat(TradeOffersReceived ?? new TradeOffer[0])
                .ToArray();
        }

        [JsonProperty("descriptions")]
        public AssetDescription[] Descriptions { get; set; }

        [JsonProperty("trade_offers_received")]
        public TradeOffer[] TradeOffersReceived { get; set; }

        [JsonProperty("trade_offers_sent")]
        public TradeOffer[] TradeOffersSent { get; set; }
    }
}