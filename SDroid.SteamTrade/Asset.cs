using System;

namespace SDroid.SteamTrade
{
    public class Asset : IEquatable<Asset>
    {
        /// <inheritdoc />
        public Asset(long appId, long contextId, long assetId, long amount = 1)
        {
            AppId = appId;
            ContextId = contextId;
            AssetId = assetId;
            Amount = amount;
        }

        public long Amount { get; }

        public long AppId { get; }

        public long AssetId { get; }

        public long ContextId { get; }

        /// <inheritdoc />
        public bool Equals(Asset other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Amount == other.Amount &&
                   AppId == other.AppId &&
                   AssetId == other.AssetId &&
                   ContextId == other.ContextId;
        }

        public static bool operator ==(Asset left, Asset right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(Asset left, Asset right)
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

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Asset) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Amount.GetHashCode();
                hashCode = (hashCode * 397) ^ AppId.GetHashCode();
                hashCode = (hashCode * 397) ^ AssetId.GetHashCode();
                hashCode = (hashCode * 397) ^ ContextId.GetHashCode();

                return hashCode;
            }
        }
    }
}