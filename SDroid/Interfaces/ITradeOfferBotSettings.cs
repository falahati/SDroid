using SDroid.SteamTrade.Models.TradeOffer;

namespace SDroid.Interfaces
{
    public interface ITradeOfferBotSettings : IBotSettingsBase
    {
        TradeOfferOptions TradeOfferOptions { get; }
    }
}