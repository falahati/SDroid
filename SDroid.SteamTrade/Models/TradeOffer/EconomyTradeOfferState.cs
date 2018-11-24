namespace SDroid.SteamTrade.Models.TradeOffer
{
    public enum TradeOfferStatus
    {
        Invalid,
        Active,
        Accepted,
        Countered,
        Expired,
        Canceled,
        Declined,
        InvalidItems,
        NeedsConfirmation,
        CanceledBySecondFactor,
        InEscrow
    }
}