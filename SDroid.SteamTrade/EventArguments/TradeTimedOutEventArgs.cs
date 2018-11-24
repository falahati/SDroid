using System;
using SteamKit2;

namespace SDroid.SteamTrade.EventArguments
{
    public class TradeTimedOutEventArgs : EventArgs
    {
        public TradeTimedOutEventArgs(SteamID partnerSteamId, DateTime? lastPartnerInteraction)
        {
            PartnerSteamId = partnerSteamId;
            LastPartnerInteraction = lastPartnerInteraction;
        }

        public DateTime? LastPartnerInteraction { get; }
        public SteamID PartnerSteamId { get; }
    }
}