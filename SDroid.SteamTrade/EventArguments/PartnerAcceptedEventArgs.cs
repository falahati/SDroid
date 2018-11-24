using System;
using SteamKit2;

namespace SDroid.SteamTrade.EventArguments
{
    public class PartnerAcceptedEventArgs : EventArgs
    {
        public PartnerAcceptedEventArgs(SteamID partnerSteamId, Asset[] myOfferedItems, Asset[] partnerOfferedItems)
        {
            PartnerSteamId = partnerSteamId;
            MyOfferedItems = myOfferedItems;
            PartnerOfferedItems = partnerOfferedItems;
        }

        public Asset[] MyOfferedItems { get; }
        public Asset[] PartnerOfferedItems { get; }
        public SteamID PartnerSteamId { get; }
    }
}