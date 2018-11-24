using SteamKit2;
using SteamKit2.Internal;

namespace SDroid.SteamKit
{
    public class ClientCommentNotificationsCallback : CallbackMsg
    {
        internal ClientCommentNotificationsCallback(JobID jobId, CMsgClientCommentNotifications body)
        {
            JobID = jobId;
            Comments = body.count_new_comments;
            ProfileComments = body.count_new_comments_owner;
            Subscriptions = body.count_new_comments_subscriptions;
        }

        public uint Comments { get; }

        public uint ProfileComments { get; }

        public uint Subscriptions { get; }
    }
}