using System;
using Newtonsoft.Json;
using SDroid.SteamTrade.InternalModels.TradeJson.Constants;

namespace SDroid.SteamTrade.InternalModels.TradeJson
{
    internal class TradeEvent : IEquatable<TradeEvent>
    {
        [JsonProperty("action")]
        public TradeEventType Action { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("appid")]
        public int AppId { get; set; }

        [JsonProperty("assetid")]
        public long AssetId { get; set; }

        [JsonProperty("contextid")]
        public long ContextId { get; set; }

        [JsonProperty("currencyid")]
        public long CurrencyId { get; set; }

        [JsonProperty("old_amount")]
        public long OldAmount { get; set; }

        [JsonProperty("steamid")]
        public ulong SteamId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("timestamp")]
        public ulong Timestamp { get; set; }

        /// <inheritdoc />
        public bool Equals(TradeEvent other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Action == other.Action &&
                   AppId == other.AppId &&
                   AssetId == other.AssetId &&
                   CurrencyId == other.CurrencyId &&
                   ContextId == other.ContextId &&
                   SteamId == other.SteamId &&
                   string.Equals(Text, other.Text) &&
                   Timestamp == other.Timestamp;
        }

        public static bool operator ==(TradeEvent left, TradeEvent right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(TradeEvent left, TradeEvent right)
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

            return Equals((TradeEvent) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) Action;
                hashCode = (hashCode * 397) ^ AppId;
                hashCode = (hashCode * 397) ^ AssetId.GetHashCode();
                hashCode = (hashCode * 397) ^ CurrencyId.GetHashCode();
                hashCode = (hashCode * 397) ^ ContextId.GetHashCode();
                hashCode = (hashCode * 397) ^ SteamId.GetHashCode();
                hashCode = (hashCode * 397) ^ (Text != null ? Text.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Timestamp.GetHashCode();

                return hashCode;
            }
        }

        public Asset GetAsset()
        {
            if (AssetId == 0)
            {
                return null;
            }

            return new Asset(AppId, ContextId, AssetId, Amount);
        }

        public Currency GetCurrency()
        {
            if (CurrencyId == 0)
            {
                return null;
            }

            return new Currency(AppId, ContextId, CurrencyId, Amount);
        }
    }
}