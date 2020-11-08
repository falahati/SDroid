using System.Linq;
using SteamKit2;
using SteamKit2.Internal;

namespace SDroid.SteamKit
{
    public class ClientUserNotificationsCallback : CallbackMsg
    {
        internal ClientUserNotificationsCallback(JobID jobId, CMsgClientUserNotifications body)
        {
            JobID = jobId;
            Notifications = body.notifications.Select(
                n => new UserNotification((int) n.count, (NotificationType) n.user_notification_type)
            ).ToArray();
        }

        public UserNotification[] Notifications { get; }
    }
}