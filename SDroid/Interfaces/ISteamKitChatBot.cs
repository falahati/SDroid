using System.Threading.Tasks;
using SteamKit2;

namespace SDroid.Interfaces
{
    public interface ISteamKitChatBot : ISteamKitBot
    {
        Task OnChatGameInvited(SteamID partnerSteamId, string message);
        Task OnChatHistoricMessageReceived(SteamID partnerSteamId, string message);
        Task OnChatMessageReceived(SteamID partnerSteamId, string message);
        Task OnChatPartnerEvent(SteamID partnerSteamId, SteamKitChatPartnerEvent chatEvent);
    }
}