using System;

namespace SDroid.SteamTrade.Exceptions
{
    public class TradeException : Exception
    {
        public TradeException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}