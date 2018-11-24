using System;
using SteamKit2;

namespace SDroid.SteamTrade.Exceptions
{
    public class BackpackFetchAssetsException : Exception
    {
        public BackpackFetchAssetsException(
            long appId,
            SteamID steamId,
            string message = null,
            Exception innerException = null)
            : base(message ?? string.Format("Failed to fetch app {0} inventory for: {1}", appId, steamId),
                innerException)
        {
            SteamId = steamId;
            AppId = appId;
        }

        public long AppId { get; }

        public SteamID SteamId { get; }
    }
}