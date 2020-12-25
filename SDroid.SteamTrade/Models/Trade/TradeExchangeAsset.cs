using System;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.Models.Trade
{
    public class TradeExchangeAsset : UserInventoryAsset, IEquatable<TradeExchangeAsset>
    {
        /// <inheritdoc />
        public TradeExchangeAsset(
            long appId,
            long contextId,
            long assetId,
            long classId,
            long instanceId,
            long newContextId,
            long newAssetId,
            long amount = 1
        ) : base(appId, newContextId, newAssetId, classId, instanceId, amount)
        {
            OriginalContextId = contextId;
            OriginalAssetId = assetId;
        }

        public long OriginalContextId { get; }
        public long OriginalAssetId { get; }

        /// <inheritdoc />
        public bool Equals(TradeExchangeAsset other)
        {
            return Equals(other as Asset);
        }

        public static bool operator ==(TradeExchangeAsset left, TradeExchangeAsset right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(TradeExchangeAsset left, TradeExchangeAsset right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as Asset);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}