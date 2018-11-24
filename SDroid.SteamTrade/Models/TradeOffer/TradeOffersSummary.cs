using SDroid.SteamTrade.InternalModels.EconomyServiceAPI;

namespace SDroid.SteamTrade.Models.TradeOffer
{
    public class TradeOffersSummary
    {
        internal TradeOffersSummary(TradeOffersSummaryResponse response)
        {
            PendingReceived = response.PendingReceivedCount;
            NewReceived = response.NewReceivedCount;
            UpdatedReceived = response.UpdatedReceivedCount;
            HistoricalReceived = response.HistoricalReceivedCount;
            PendingSent = response.PendingSentCount;
            NewlyAcceptedSent = response.NewlyAcceptedSentCount;
            UpdatedSent = response.UpdatedSentCount;
            HistoricalSent = response.HistoricalSentCount;
        }

        public int HistoricalReceived { get; }

        public int HistoricalSent { get; }

        public int NewlyAcceptedSent { get; }

        public int NewReceived { get; }
        public int PendingReceived { get; }

        public int PendingSent { get; }

        public int UpdatedReceived { get; }

        public int UpdatedSent { get; }
    }
}