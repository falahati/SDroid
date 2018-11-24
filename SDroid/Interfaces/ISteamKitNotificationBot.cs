using System.Threading.Tasks;
using SDroid.SteamKit;

namespace SDroid.Interfaces
{
    public interface ISteamKitNotificationBot
    {
        Task OnClientNotifications(int comments, int profileComments, int subscriptions);
        Task OnUserNotifications(UserNotification[] notifications);
    }
}