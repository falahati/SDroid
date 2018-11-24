using System;
using Newtonsoft.Json;

namespace SDroid.SteamMobile
{
    /// <summary>
    ///     Represents a confirmation request waiting to be responded
    /// </summary>
    public class Confirmation : IEquatable<Confirmation>
    {
        // ReSharper disable once TooManyDependencies
        [JsonConstructor]
        public Confirmation(ulong id, ulong key, ConfirmationType type, ulong creator)
        {
            Id = id;
            Key = key;
            Creator = creator;

            // Doesn't matter if we are not sure about all confirmation types. 
            // Probably so as the library user. And it is always possible to convert to int.
            Type = type;
        }

        /// <summary>
        ///     Gets the an identification number either the Trade Offer or market transaction that caused this confirmation to be
        ///     created.
        /// </summary>
        public ulong Creator { get; }

        /// <summary>
        ///     Gets the identification number of this confirmation.
        /// </summary>
        public ulong Id { get; }

        /// <summary>
        ///     Gets the unique key used to act upon this confirmation.
        /// </summary>
        public ulong Key { get; }

        /// <summary>
        ///     Gets the type of this confirmation.
        /// </summary>
        public ConfirmationType Type { get; }

        /// <inheritdoc />
        public bool Equals(Confirmation other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Creator == other.Creator && Id == other.Id && Key == other.Key && Type == other.Type;
        }

        public static bool operator ==(Confirmation left, Confirmation right)
        {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(Confirmation left, Confirmation right)
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

            return Equals(obj as Confirmation);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Creator.GetHashCode();
                hashCode = (hashCode * 397) ^ Id.GetHashCode();
                hashCode = (hashCode * 397) ^ Key.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Type;

                return hashCode;
            }
        }
    }
}