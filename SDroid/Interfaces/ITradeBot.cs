using System.Threading.Tasks;
using SDroid.SteamTrade;
using SteamKit2;

namespace SDroid.Interfaces
{
    public interface ITradeBot
    {
        ITradeBotSettings TradeBotSettings { get; }
        TradeManager TradeManager { get; set; }
        Task OnTradeCreated(SteamID partnerSteamId, Trade trade);
    }
}