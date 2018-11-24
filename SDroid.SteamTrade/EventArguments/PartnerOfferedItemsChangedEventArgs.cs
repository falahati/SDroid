using System;
using SteamKit2;

namespace SDroid.SteamTrade.EventArguments
{
    public class PartnerOfferedItemsChangedEventArgs : EventArgs
    {
        public PartnerOfferedItemsChangedEventArgs(
            SteamID partnerSteamId,
            PartnerOfferedItemsChangedAction action,
            Asset asset)
        {
            PartnerSteamId = partnerSteamId;
            Action = action;
            Asset = asset;
        }

        public PartnerOfferedItemsChangedAction Action { get; }
        public Asset Asset { get; }
        public SteamID PartnerSteamId { get; }
    }
}