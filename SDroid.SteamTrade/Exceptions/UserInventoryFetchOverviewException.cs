using System;
using SteamKit2;

namespace SDroid.SteamTrade.Exceptions
{
    public class UserInventoryFetchOverviewException : Exception
    {
        public UserInventoryFetchOverviewException()
        {
        }


        public UserInventoryFetchOverviewException(SteamID steamId, Exception innerException = null)
            : base(string.Format("Failed to fetch user {0} inventory overview.", steamId), innerException)
        {
            SteamId = steamId;
        }

        public SteamID SteamId { get; }
    }
}