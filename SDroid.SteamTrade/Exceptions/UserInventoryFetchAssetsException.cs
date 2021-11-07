using System;
using SteamKit2;

namespace SDroid.SteamTrade.Exceptions
{
    public class UserInventoryFetchAssetsException : Exception
    {
        public UserInventoryFetchAssetsException()
        {
        }

        public UserInventoryFetchAssetsException(
            long appId,
            long contextId,
            SteamID steamId,
            Exception innerException = null)
            : base($"Failed to fetch user {steamId.ConvertToUInt64()} app {appId}, context {contextId} inventory.",
                innerException)
        {
            SteamId = steamId;
            AppId = appId;
            ContextId = contextId;
        }

        public long AppId { get; }
        public long ContextId { get; }

        public SteamID SteamId { get; }
    }
}