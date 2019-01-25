using System;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.Models.TradeOffer
{
    public class TradeOfferAsset : UserInventoryAsset, IEquatable<TradeOfferAsset>
    {
        /// <inheritdoc />
        public TradeOfferAsset(
            long appId,
            long contextId,
            long assetId,
            long classId,
            long instanceId,
            bool isMissing,
            long amount = 1) : base(appId, contextId, assetId, classId, instanceId, amount)
        {
            IsMissing = isMissing;
        }

        public bool IsMissing { get; }

        /// <inheritdoc />
        public bool Equals(TradeOfferAsset other)
        {
            return Equals(other as Asset);
        }

        public static bool operator ==(TradeOfferAsset left, TradeOfferAsset right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(TradeOfferAsset left, TradeOfferAsset right)
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