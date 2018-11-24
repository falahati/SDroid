using System;

namespace SDroid.SteamTrade.Exceptions
{
    public class TradeOfferException : Exception
    {
        public TradeOfferException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}