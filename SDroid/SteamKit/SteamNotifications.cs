using SteamKit2;
using SteamKit2.Internal;

namespace SDroid.SteamKit
{
    // ReSharper disable once HollowTypeName
    public class SteamNotifications : ClientMsgHandler
    {
        internal SteamNotifications()
        {
        }

        public override void HandleMsg(IPacketMsg packetMsg)
        {
            switch (packetMsg.MsgType)
            {
                case EMsg.ClientNewLoginKey:
                    RequestClientNotifications();
                    RequestUserNotifications();

                    break;

                case EMsg.ClientUserNotifications:
                    Client.PostCallback(
                        new ClientUserNotificationsCallback(
                            packetMsg.SourceJobID,
                            new ClientMsgProtobuf<CMsgClientUserNotifications>(packetMsg).Body
                        )
                    );

                    break;

                case EMsg.ClientCommentNotifications:
                    Client.PostCallback(
                        new ClientCommentNotificationsCallback(
                            packetMsg.SourceJobID,
                            new ClientMsgProtobuf<CMsgClientCommentNotifications>(packetMsg).Body
                        )
                    );

                    break;
            }
        }

        /// <summary>
        ///     Request to see if the client user has any comment notifications
        /// </summary>
        public AsyncJob<ClientCommentNotificationsCallback> RequestClientNotifications()
        {
            var reqMsg = new ClientMsgProtobuf<CMsgClientRequestCommentNotifications>(
                EMsg.ClientRequestCommentNotifications
            )
            {
                //SourceJobID = Client.GetNextJobID()
            };

            Client.Send(reqMsg);
            return null;
            //return new AsyncJob<ClientCommentNotificationsCallback>(Client, reqMsg.SourceJobID);
        }

        /// <summary>
        ///     Request to see if the client user has any notifications.
        /// </summary>
        public AsyncJob<ClientUserNotificationsCallback> RequestUserNotifications()
        {
            var reqMsg =
                new ClientMsgProtobuf<CMsgClientRequestItemAnnouncements>(
                    EMsg.ClientRequestItemAnnouncements
                )
                {
                    //SourceJobID = Client.GetNextJobID()
                };
            Client.Send(reqMsg);

            return null;
            // return new AsyncJob<ClientUserNotificationsCallback>(Client, reqMsg.SourceJobID);
        }
    }
}