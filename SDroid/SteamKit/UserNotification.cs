using Newtonsoft.Json;

namespace SDroid.SteamKit
{
    public class UserNotification
    {
        [JsonConstructor]
        public UserNotification(int count, NotificationType notificationType)
        {
            Count = count;
            Type = notificationType;
        }

        public int Count { get; }

        public NotificationType Type { get; }
    }
}