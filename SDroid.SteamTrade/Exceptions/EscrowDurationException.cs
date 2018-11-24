using System;

namespace SDroid.SteamTrade.Exceptions
{
    public class EscrowDurationException : Exception
    {
        public EscrowDurationException() : base(
            "Could not extract or parse the escrow duration. Trade is invalid or the offer request rejected.")
        {
        }
    }
}