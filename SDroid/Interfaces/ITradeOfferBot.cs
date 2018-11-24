using System.Threading.Tasks;
using SDroid.SteamTrade;

namespace SDroid.Interfaces
{
    public interface ITradeOfferBot
    {
        ITradeOfferBotSettings TradeOfferBotSettings { get; }
        TradeOfferManager TradeOfferManager { get; set; }
        Task OnTradeOfferAccepted(TradeOffer tradeOffer);
        Task OnTradeOfferCanceled(TradeOffer tradeOffer);
        Task OnTradeOfferChanged(TradeOffer tradeOffer);
        Task OnTradeOfferDeclined(TradeOffer tradeOffer);
        Task OnTradeOfferInEscrow(TradeOffer tradeOffer);
        Task OnTradeOfferNeedsConfirmation(TradeOffer tradeOffer);
        Task OnTradeOfferReceived(TradeOffer tradeOffer);
        Task OnTradeOfferSent(TradeOffer tradeOffer);
    }
}