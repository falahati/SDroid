using System;

namespace SDroid.SteamTrade.Exceptions
{
    public class BackpackFetchSchemaException : Exception
    {
        public BackpackFetchSchemaException(long appId, string message = null, Exception innerException = null)
            : base(message ?? string.Format("Failed to retrieve item schema for appId {0}.", appId), innerException)
        {
            AppId = appId;
        }

        public long AppId { get; }
    }
}