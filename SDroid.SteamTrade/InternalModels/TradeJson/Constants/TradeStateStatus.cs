namespace SDroid.SteamTrade.Models.Trade
{
    internal enum TradeStateStatus
    {
        OnGoing = 0,
        Completed = 1,
        Empty = 2,
        Canceled = 3,
        SessionExpired = 4,
        Failed = 5,
        PendingConfirmation = 6
    }
}