namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI.Constants
{
    internal enum EconomyTradeOfferState
    {
        Invalid = 1,
        Active = 2,
        Accepted = 3,
        Countered = 4,
        Expired = 5,
        Canceled = 6,
        Declined = 7,
        InvalidItems = 8,
        NeedsConfirmation = 9,
        CanceledBySecondFactor = 10,
        InEscrow = 11,
        Unknown = 0
    }
}