using System;
using SteamKit2;

namespace SDroid.SteamTrade.EventArguments
{
    public class TradeCreatedEventArgs : EventArgs
    {
        public TradeCreatedEventArgs(
            SteamID partnerSteamId,
            Trade trade)
        {
            PartnerSteamId = partnerSteamId;
            Trade = trade;
        }

        public SteamID PartnerSteamId { get; }
        public Trade Trade { get; }
    }
}