namespace SDroid.SteamTrade.Models.TradeOffer
{
    public enum TradeOfferStatus
    {
        Invalid = 0,
        Active = 1,
        Accepted = 2,
        Countered = 3,
        Expired = 4,
        Canceled = 5,
        Declined = 6,
        InvalidItems = 7,
        NeedsConfirmation = 8,
        CanceledBySecondFactor = 9,
        InEscrow = 10
    }
}