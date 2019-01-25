using System;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.Models.UserInventory
{
    public class UserInventoryAssetDescription : IEquatable<UserInventoryAssetDescription>
    {
        [JsonConstructor]
        internal UserInventoryAssetDescription(
            long appId,
            long classId,
            long instanceId,
            string iconUrl,
            string iconUrlLarge,
            string name,
            string marketHashName,
            string marketName,
            string type,
            bool isTradable,
            bool isMarketable,
            bool isCommodity,
            bool isCurrency,
            UserInventoryAssetDescriptionEntry[] descriptions,
            UserInventoryAssetDescriptionTag[] tags)
        {
            AppId = appId;
            ClassId = classId;
            InstanceId = instanceId;
            IconUrl = iconUrl;
            IconUrlLarge = iconUrlLarge;
            Name = name;
            MarketHashName = marketHashName;
            MarketName = marketName;
            Type = type;
            IsTradable = isTradable;
            IsMarketable = isMarketable;
            IsCurrency = isCurrency;
            Descriptions = descriptions;
            Tags = tags;
            IsCommodity = isCommodity;
        }

        public long AppId { get; }

        public long ClassId { get; }

        public UserInventoryAssetDescriptionEntry[] Descriptions { get; }

        public string IconUrl { get; }

        public string IconUrlLarge { get; }

        public long InstanceId { get; }

        public bool IsCommodity { get; }

        public bool IsCurrency { get; }

        public bool IsMarketable { get; }

        public bool IsTradable { get; }

        public string MarketHashName { get; }

        public string MarketName { get; }

        public string Name { get; }
        public UserInventoryAssetDescriptionTag[] Tags { get; }

        public string Type { get; }

        /// <inheritdoc />
        public bool Equals(UserInventoryAssetDescription other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return AppId == other.AppId && ClassId == other.ClassId && InstanceId == other.InstanceId;
        }

        public static bool operator ==(UserInventoryAssetDescription left, UserInventoryAssetDescription right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(UserInventoryAssetDescription left, UserInventoryAssetDescription right)
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

            return Equals((UserInventoryAssetDescription) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AppId.GetHashCode();
                hashCode = (hashCode * 397) ^ ClassId.GetHashCode();
                hashCode = (hashCode * 397) ^ InstanceId.GetHashCode();

                return hashCode;
            }
        }

        public bool DoesDescribe(UserInventoryAsset asset)
        {
            return asset.AppId == AppId && asset.ClassId == ClassId && asset.InstanceId == InstanceId;
        }
    }
}