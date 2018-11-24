using System.Linq;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.TradeOfferJson
{
    internal class TradeOfferPartyState
    {
        public TradeOfferPartyState() : this(new Asset[0])
        {
        }

        public TradeOfferPartyState(Asset[] assets)
        {
            Assets = assets.Select(TradeOfferAsset.FromAsset).ToArray();
            IsReady = false;
            Currency = new TradeOfferAsset[0];
        }

        [JsonProperty("assets")]
        public TradeOfferAsset[] Assets { get; set; }

        [JsonProperty("currency")]
        public TradeOfferAsset[] Currency { get; set; }

        [JsonProperty("ready")]
        public bool IsReady { get; set; }
    }
}