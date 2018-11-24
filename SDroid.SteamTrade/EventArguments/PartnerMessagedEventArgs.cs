using System;
using SteamKit2;

namespace SDroid.SteamTrade.EventArguments
{
    public class PartnerMessagedEventArgs : EventArgs
    {
        public PartnerMessagedEventArgs(SteamID partnerSteamId, string message, DateTime dateTime)
        {
            PartnerSteamId = partnerSteamId;
            Message = message;
            DateTime = dateTime;
        }

        public DateTime DateTime { get; }
        public string Message { get; }

        public SteamID PartnerSteamId { get; }
    }
}