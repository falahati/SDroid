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
            : base($"Failed to fetch user {steamId.ConvertToUInt64()} inventory overview.", innerException)
        {
            SteamId = steamId;
        }

        public SteamID SteamId { get; }
    }
}