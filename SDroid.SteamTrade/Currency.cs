using System;

namespace SDroid.SteamTrade
{
    public class Currency : IEquatable<Currency>
    {
        /// <inheritdoc />
        public Currency(long appId, long contextId, long currencyId, long amount = 1)
        {
            AppId = appId;
            ContextId = contextId;
            CurrencyId = currencyId;
            Amount = amount;
        }

        public long Amount { get; }

        public long AppId { get; }

        public long ContextId { get; }

        public long CurrencyId { get; }

        /// <inheritdoc />
        public bool Equals(Currency other)
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
                   CurrencyId == other.CurrencyId &&
                   ContextId == other.ContextId;
        }

        public static bool operator ==(Currency left, Currency right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(Currency left, Currency right)
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

            return Equals((Currency) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Amount.GetHashCode();
                hashCode = (hashCode * 397) ^ AppId.GetHashCode();
                hashCode = (hashCode * 397) ^ CurrencyId.GetHashCode();
                hashCode = (hashCode * 397) ^ ContextId.GetHashCode();

                return hashCode;
            }
        }
    }
}