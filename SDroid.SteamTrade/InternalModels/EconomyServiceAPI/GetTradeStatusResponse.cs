using System.Collections.Generic;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class GetTradeStatusResponse
    {
        [JsonProperty("trades")]
        public List<TradeExchangeStatus> Trades { get; set; }

        [JsonProperty("descriptions")]
        public List<TradeExchangeStatusAssetDescription> Descriptions { get; set; }
    }
}
