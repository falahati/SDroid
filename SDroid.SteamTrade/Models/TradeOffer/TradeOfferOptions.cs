using System;
using SDroid.SteamWeb;

namespace SDroid.SteamTrade.Models.TradeOffer
{
    public class TradeOfferOptions : OperationRetryHelper
    {
        private static TradeOfferOptions _default = new TradeOfferOptions();

        public TradeOfferOptions(
            int numberOfTries = 3,
            TimeSpan? requestDelay = null,
            TimeSpan? pollInterval = null) : base(numberOfTries, requestDelay)
        {
            PollInterval = pollInterval ?? TimeSpan.FromSeconds(20);
        }

        public new static TradeOfferOptions Default
        {
            get => _default;
            set => _default = value ?? new TradeOfferOptions();
        }

        public TimeSpan PollInterval { get; }
    }
}