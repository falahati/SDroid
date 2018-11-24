using System;

namespace SDroid.SteamTrade.Models.UserInventory
{
    public class UserInventoryAsset : Asset, IEquatable<UserInventoryAsset>
    {
        /// <inheritdoc />
        public UserInventoryAsset(
            long appId,
            long contextId,
            long assetId,
            long classId,
            long instanceId,
            long amount = 1) : base(appId, contextId, assetId, amount)
        {
            ClassId = classId;
            InstanceId = instanceId;
        }

        public long ClassId { get; }

        public long InstanceId { get; }

        /// <inheritdoc />
        public bool Equals(UserInventoryAsset other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return base.Equals(other) && ClassId == other.ClassId && InstanceId == other.InstanceId;
        }

        public static bool operator ==(UserInventoryAsset left, UserInventoryAsset right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(UserInventoryAsset left, UserInventoryAsset right)
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

            if (obj is UserInventoryAsset inventoryItem)
            {
                return Equals(inventoryItem);
            }

            return Equals(obj as Asset);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ ClassId.GetHashCode();
                hashCode = (hashCode * 397) ^ InstanceId.GetHashCode();

                return hashCode;
            }
        }

        public bool IsOfSameType(UserInventoryAsset other)
        {
            return AppId == other.AppId &&
                   ContextId == other.ContextId &&
                   ClassId == other.ClassId &&
                   InstanceId == other.InstanceId;
        }
    }
}