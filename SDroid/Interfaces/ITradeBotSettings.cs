using SDroid.SteamTrade.Models.Trade;

namespace SDroid.Interfaces
{
    public interface ITradeBotSettings : IBotSettingsBase
    {
        TradeOptions TradeOptions { get; set; }
    }
}