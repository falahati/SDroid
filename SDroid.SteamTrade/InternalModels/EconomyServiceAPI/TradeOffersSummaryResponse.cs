using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class TradeOffersSummaryResponse
    {
        [JsonProperty("historical_received_count")]
        public int HistoricalReceivedCount { get; set; }

        [JsonProperty("historical_sent_count")]
        public int HistoricalSentCount { get; set; }

        [JsonProperty("newly_accepted_sent_count")]
        public int NewlyAcceptedSentCount { get; set; }

        [JsonProperty("new_received_count")]
        public int NewReceivedCount { get; set; }

        [JsonProperty("pending_received_count")]
        public int PendingReceivedCount { get; set; }

        [JsonProperty("pending_sent_count")]
        public int PendingSentCount { get; set; }

        [JsonProperty("updated_received_count")]
        public int UpdatedReceivedCount { get; set; }

        [JsonProperty("updated_sent_count")]
        public int UpdatedSentCount { get; set; }
    }
}