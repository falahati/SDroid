using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.TradeOfferJson
{
    internal class TradeOfferState
    {
        public TradeOfferState() : this(new Asset[0], new Asset[0])
        {
        }

        public TradeOfferState(Asset[] ourItems, Asset[] theirItems, int version = 1)
        {
            Version = version;
            NewVersion = version > 1;
            OurItems = new TradeOfferPartyState(ourItems);
            TheirItems = new TradeOfferPartyState(theirItems);
        }

        [JsonProperty("newversion")]
        public bool NewVersion { get; private set; }

        [JsonProperty("me")]
        public TradeOfferPartyState OurItems { get; set; }

        [JsonProperty("them")]
        public TradeOfferPartyState TheirItems { get; set; }

        [JsonProperty("version")]
        private int Version { get; set; }
    }
}