using System;
using SteamKit2;

namespace SDroid.SteamTrade.EventArguments
{
    public class TradeEndedEventArgs : EventArgs
    {
        public TradeEndedEventArgs(
            SteamID partnerSteamId,
            bool isCompleted,
            bool isCanceled,
            bool doesNeedConfirmation,
            long? tradeId,
            Asset[] myOfferedItems,
            Asset[] partnerOfferedItems)
        {
            PartnerSteamId = partnerSteamId;
            IsCompleted = isCompleted;
            IsCanceled = isCanceled;
            DoesNeedConfirmation = doesNeedConfirmation;
            TradeId = tradeId;
            MyOfferedItems = myOfferedItems;
            PartnerOfferedItems = partnerOfferedItems;
        }

        public bool DoesNeedConfirmation { get; }
        public bool IsCanceled { get; }

        public bool IsCompleted { get; }
        public Asset[] MyOfferedItems { get; }
        public Asset[] PartnerOfferedItems { get; }
        public SteamID PartnerSteamId { get; }
        public long? TradeId { get; }
    }
}