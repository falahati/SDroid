using System.Collections.Generic;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class GetTradeOfferResponse
    {
        [JsonProperty("descriptions")]
        public List<AssetDescription> Descriptions { get; set; }

        [JsonProperty("offer")]
        public TradeOffer Offer { get; set; }
    }
}