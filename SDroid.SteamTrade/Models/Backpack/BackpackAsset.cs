using System;

namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAsset : Asset, IEquatable<BackpackAsset>
    {
        public BackpackAsset(long appId, long contextId, long assetId, int definitionIndex, long amount = 1) : base(
            appId, contextId, assetId, amount)
        {
            DefinitionIndex = definitionIndex;
        }

        public int DefinitionIndex { get; }

        public bool Equals(BackpackAsset other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return base.Equals(other) && DefinitionIndex == other.DefinitionIndex;
        }

        public static bool operator ==(BackpackAsset left, BackpackAsset right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(BackpackAsset left, BackpackAsset right)
        {
            return !(left == right);
        }

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

            if (obj is BackpackAsset inventoryItem)
            {
                return Equals(inventoryItem);
            }

            return Equals(obj as Asset);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ DefinitionIndex.GetHashCode();

                return hashCode;
            }
        }
    }
}