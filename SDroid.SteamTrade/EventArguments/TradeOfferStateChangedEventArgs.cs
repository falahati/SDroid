using System;

namespace SDroid.SteamTrade.EventArguments
{
    public class TradeOfferStateChangedEventArgs : EventArgs
    {
        public TradeOfferStateChangedEventArgs(TradeOffer tradeOffer)
        {
            TradeOffer = tradeOffer;
        }

        public TradeOffer TradeOffer { get; }
    }
}