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
            : base(string.Format("Failed to fetch user {0} app {1}, context {2} inventory.", steamId, appId, contextId),
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