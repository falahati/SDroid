using System;
using SteamKit2;

namespace SDroid.SteamTrade.EventArguments
{
    public class PartnerStatusChangedEventArgs : EventArgs
    {
        public PartnerStatusChangedEventArgs(
            SteamID partnerSteamId,
            bool isInTrade,
            bool isFirstConnection,
            bool isTimeOut)
        {
            PartnerSteamId = partnerSteamId;
            IsInTrade = isInTrade;
            IsFirstConnection = isFirstConnection;
            IsTimeOut = isTimeOut;
        }

        public bool IsFirstConnection { get; }

        public bool IsInTrade { get; }
        public bool IsTimeOut { get; }

        public SteamID PartnerSteamId { get; }
    }
}