using System;
using SDroid.SteamWeb;

namespace SDroid.SteamTrade.Models.Trade
{
    public class TradeOptions : OperationRetryHelper
    {
        private static TradeOptions _default = new TradeOptions();

        public TradeOptions(
            int numberOfTries = 3,
            TimeSpan? requestDelay = null,
            TimeSpan? partnerTimeOut = null,
            TimeSpan? pollTimeoutValue = null,
            TimeSpan? pollInterval = null) : base(numberOfTries, requestDelay)
        {
            TradeTimeOut = partnerTimeOut ?? TimeSpan.FromSeconds(120);
            PollTimeOut = pollTimeoutValue ?? TimeSpan.FromSeconds(60);
            PollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
        }

        public new static TradeOptions Default
        {
            get => _default;
            set => _default = value ?? new TradeOptions();
        }

        public TimeSpan PollInterval { get; }
        public TimeSpan PollTimeOut { get; }

        public TimeSpan TradeTimeOut { get; }
    }
}