using System;

namespace SDroid.SteamTrade
{
    public class EscrowDuration
    {
        internal EscrowDuration(TimeSpan myEscrowDuration, TimeSpan partnerEscrowDuration)
        {
            MyEscrowDuration = myEscrowDuration;
            PartnerEscrowDuration = partnerEscrowDuration;
        }

        public TimeSpan MyEscrowDuration { get; }
        public TimeSpan PartnerEscrowDuration { get; }
    }
}