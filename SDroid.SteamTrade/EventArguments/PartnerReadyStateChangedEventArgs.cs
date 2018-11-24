using System;
using SteamKit2;

namespace SDroid.SteamTrade.EventArguments
{
    public class PartnerReadyStateChangedEventArgs : EventArgs
    {
        public PartnerReadyStateChangedEventArgs(SteamID partnerSteamId, bool isPartnerReady)
        {
            PartnerSteamId = partnerSteamId;
            IsPartnerReady = isPartnerReady;
        }

        public bool IsPartnerReady { get; }
        public SteamID PartnerSteamId { get; }
    }
}