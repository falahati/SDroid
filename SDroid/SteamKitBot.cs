using System;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SDroid.Helpers;
using SDroid.Interfaces;
using SDroid.SteamKit;
using SDroid.SteamMobile;
using SteamKit2;

namespace SDroid
{
    public abstract class SteamKitBot : SteamBot
    {
        protected ExponentialBackoff ConnectionBackoff = new ExponentialBackoff();
        protected ExponentialBackoff LoginBackoff = new ExponentialBackoff();

        protected Timer LoginCheckTimer;

        protected SteamKitBot(ISteamKitBotSettings settings, IBotLogger botLogger) : base(settings, botLogger)
        {
            CancellationTokenSource = new CancellationTokenSource();

            SteamClient = new SteamClient();
            SteamNotifications = new SteamNotifications();
            SteamClient.AddHandler(SteamNotifications);
            SteamUser = SteamClient.GetHandler<SteamUser>();
            SteamFriends = SteamClient.GetHandler<SteamFriends>();

            CallbackManager = new CallbackManager(SteamClient);

            CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnInternalSteamClientConnected);
            CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnInternalSteamClientDisconnect);

            CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnInternalSteamUserLoggedOn);
            CallbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnInternalSteamUserLoggedOff);
            //CallbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnInternalSteamUserLoginKeyExchange);
            CallbackManager.Subscribe<SteamUser.WebAPIUserNonceCallback>(OnInternalSteamUserNewWebApiUserNonce);
            CallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(
                OnInternalSteamUserUpdateMachineAuthenticationCallback);

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ISteamKitChatBot)
            {
                CallbackManager.Subscribe<SteamFriends.FriendMsgCallback>(OnInternalFriendSteamFriendsMessageReceived);
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ISteamKitNotificationBot)
            {
                CallbackManager.Subscribe<ClientCommentNotificationsCallback>(OnInternalCommentNotifications);
                CallbackManager.Subscribe<ClientUserNotificationsCallback>(OnInternalUserNotifications);
            }
        }

        protected new ISteamKitBotSettings BotSettings
        {
            get => base.BotSettings as ISteamKitBotSettings;
        }

        protected CallbackManager CallbackManager { get; set; }
        protected SteamClient SteamClient { get; set; }
        protected SteamFriends SteamFriends { get; set; }
        protected SteamNotifications SteamNotifications { get; set; }
        protected SteamUser SteamUser { get; set; }

        public override Task StartBot()
        {
            lock (this)
            {
                if (BotStatus != SteamBotStatus.Ready)
                {
                    return Task.CompletedTask;
                }

                BotStatus = SteamBotStatus.Running;
            }

            CancellationTokenSource = new CancellationTokenSource();

            var _ = Task.Factory.StartNew(SteamKitPolling, CancellationTokenSource.Token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously);

            SteamClient.Connect();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task StopBot()
        {
            SteamClient.Disconnect();

            return base.StopBot();
        }

        /// <inheritdoc />
        protected override Task BotLogin()
        {
            lock (this)
            {
                if (BotStatus == SteamBotStatus.LoggingIn)
                {
                    return Task.CompletedTask;
                }

                BotStatus = SteamBotStatus.LoggingIn;
            }

            InternalInitializeLogin();

            return Task.CompletedTask;
        }

        protected override async Task OnCheckSession()
        {
            try
            {
                lock (this)
                {
                    if (BotStatus != SteamBotStatus.Running || WebAccess == null)
                    {
                        return;
                    }
                }

                if (!await WebAccess.VerifySession().ConfigureAwait(false))
                {
                    await BotLogger.Warning("OnCheckSession", "Session expired. Requesting new WebAPI user nonce.")
                        .ConfigureAwait(false);

                    await SteamUser.RequestWebAPIUserNonce().ToTask().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                await BotLogger.Error("OnCheckSession", e.Message).ConfigureAwait(false);
                await OnLoggedOut().ConfigureAwait(false);
                await OnTerminate().ConfigureAwait(false);
            }
        }

        protected virtual Task OnConnected()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnDisconnected()
        {
            return Task.CompletedTask;
        }

        private void InternalInitializeLogin(SteamUser.LogOnDetails loginDetails = null)
        {
            LoginBackoff.Delay().Wait();
            OnLoggingIn().Wait();

            var timerLoginDetails = loginDetails;
            LoginCheckTimer = new Timer(state =>
            {
                lock (this)
                {
                    if (BotStatus != SteamBotStatus.LoggingIn)
                    {
                        return;
                    }
                }

                BotLogger.Warning("LoginCheckTimer", "Login stalled, trying again.");
                InternalInitializeLogin(timerLoginDetails);
            }, null, TimeSpan.FromSeconds(BotSettings.LoginTimeout), TimeSpan.FromMilliseconds(-1));

            loginDetails = loginDetails ??
                           new SteamUser.LogOnDetails
                           {
                               Username = BotSettings.Username,
                               Password = BotSettings.Password,
                               SentryFileHash = BotSettings.MachineHash?.Length > 0 ? BotSettings.MachineHash : null
                           };
            SteamUser.LogOn(loginDetails);
        }

        private void OnInternalCommentNotifications(
            ClientCommentNotificationsCallback clientCommentNotificationsCallback)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ISteamKitNotificationBot notificationBot)
            {
                notificationBot.OnClientNotifications(
                        (int) clientCommentNotificationsCallback.Comments,
                        (int) clientCommentNotificationsCallback.ProfileComments,
                        (int) clientCommentNotificationsCallback.Subscriptions)
                    .Wait();
            }
        }

        private void OnInternalFriendSteamFriendsMessageReceived(SteamFriends.FriendMsgCallback friendMsgCallback)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ISteamKitChatBot chatBot)
            {
                switch (friendMsgCallback.EntryType)
                {
                    case EChatEntryType.ChatMsg:
                        chatBot.OnChatMessageReceived(friendMsgCallback.Sender, friendMsgCallback.Message);

                        break;
                    case EChatEntryType.HistoricalChat:
                        chatBot.OnChatHistoricMessageReceived(friendMsgCallback.Sender, friendMsgCallback.Message);

                        break;
                    case EChatEntryType.InviteGame:
                        chatBot.OnChatGameInvited(friendMsgCallback.Sender, friendMsgCallback.Message).Wait();

                        break;
                    case EChatEntryType.Typing:
                        chatBot.OnChatPartnerEvent(friendMsgCallback.Sender, SteamKitChatPartnerEvent.Typing).Wait();

                        break;
                    case EChatEntryType.WasKicked:
                        chatBot.OnChatPartnerEvent(friendMsgCallback.Sender, SteamKitChatPartnerEvent.Kicked).Wait();
                        chatBot.OnChatPartnerEvent(friendMsgCallback.Sender, SteamKitChatPartnerEvent.LeftChat).Wait();

                        break;
                    case EChatEntryType.WasBanned:
                        chatBot.OnChatPartnerEvent(friendMsgCallback.Sender, SteamKitChatPartnerEvent.Banned).Wait();
                        chatBot.OnChatPartnerEvent(friendMsgCallback.Sender, SteamKitChatPartnerEvent.LeftChat).Wait();

                        break;
                    case EChatEntryType.Disconnected:
                        chatBot.OnChatPartnerEvent(friendMsgCallback.Sender, SteamKitChatPartnerEvent.Disconnected)
                            .Wait();
                        chatBot.OnChatPartnerEvent(friendMsgCallback.Sender, SteamKitChatPartnerEvent.LeftChat).Wait();

                        break;
                    case EChatEntryType.LeftConversation:
                        chatBot.OnChatPartnerEvent(friendMsgCallback.Sender, SteamKitChatPartnerEvent.LeftChat).Wait();

                        break;
                    case EChatEntryType.Entered:
                        chatBot.OnChatPartnerEvent(friendMsgCallback.Sender, SteamKitChatPartnerEvent.EnteredChat)
                            .Wait();

                        break;
                    case EChatEntryType.LinkBlocked:
                        chatBot.OnChatPartnerEvent(friendMsgCallback.Sender, SteamKitChatPartnerEvent.LinkBlocked)
                            .Wait();

                        break;
                }
            }
        }

        private void OnInternalSteamClientConnected(SteamClient.ConnectedCallback connectedCallback)
        {
            ConnectionBackoff.Reset();
            OnConnected().Wait();
        }

        private void OnInternalSteamClientDisconnect(SteamClient.DisconnectedCallback disconnectedCallback)
        {
            if (!disconnectedCallback.UserInitiated)
            {
                lock (this)
                {
                    if (BotStatus == SteamBotStatus.Faulted)
                    {
                        return;
                    }

                    BotStatus = SteamBotStatus.Connecting;
                }

                OnDisconnected().Wait();
                ConnectionBackoff.Delay().Wait();
                SteamClient.Connect();
            }
            else
            {
                OnDisconnected().Wait();
            }
        }

        private void OnInternalSteamUserLoggedOff(SteamUser.LoggedOffCallback loggedOffCallback)
        {
            BotLogger.Debug("OnInternalSteamUserLoggedOff", "SteamUser.LoggedOffCallback.Result = `{0}`",
                loggedOffCallback.Result);

            lock (this)
            {
                if (BotStatus == SteamBotStatus.Faulted)
                {
                    return;
                }
            }

            if (EResult.Invalid == loggedOffCallback.Result ||
                EResult.NoConnection == loggedOffCallback.Result ||
                EResult.Busy == loggedOffCallback.Result ||
                EResult.Timeout == loggedOffCallback.Result ||
                EResult.ServiceUnavailable == loggedOffCallback.Result ||
                EResult.NotLoggedOn == loggedOffCallback.Result ||
                EResult.Pending == loggedOffCallback.Result ||
                EResult.Revoked == loggedOffCallback.Result ||
                EResult.Expired == loggedOffCallback.Result ||
                EResult.LogonSessionReplaced == loggedOffCallback.Result ||
                EResult.ConnectFailed == loggedOffCallback.Result ||
                EResult.HandshakeFailed == loggedOffCallback.Result ||
                EResult.IOFailure == loggedOffCallback.Result ||
                EResult.RemoteDisconnect == loggedOffCallback.Result ||
                EResult.ServiceReadOnly == loggedOffCallback.Result ||
                EResult.Suspended == loggedOffCallback.Result ||
                EResult.Cancelled == loggedOffCallback.Result ||
                EResult.BadResponse == loggedOffCallback.Result ||
                EResult.UnexpectedError == loggedOffCallback.Result ||
                EResult.InvalidCEGSubmission == loggedOffCallback.Result ||
                EResult.AccountLoginDeniedThrottle == loggedOffCallback.Result)
            {
                InternalInitializeLogin();
            }
            else
            {
                OnLoggedOut().Wait();
            }
        }

        // ReSharper disable once MethodTooLong
        // ReSharper disable once ExcessiveIndentation
        private void OnInternalSteamUserLoggedOn(SteamUser.LoggedOnCallback loggedOnCallback)
        {
            lock (this)
            {
                if (BotStatus != SteamBotStatus.LoggingIn)
                {
                    return;
                }
            }

            if (loggedOnCallback.Result == EResult.OK)
            {
                var session = SteamClient.AuthenticateWebSession(WebAPI, loggedOnCallback.WebAPIUserNonce).Result;

                if (session != null)
                {
                    LoginBackoff.Reset();
                    OnNewWebSessionAvailable(session).Wait();
                }

                InternalInitializeLogin();

                return;
            }

            BotLogger.Debug("OnInternalSteamUserLoggedOn", "SteamUser.LoggedOnCallback.Result = `{0}`",
                loggedOnCallback.Result);

            var loginDetails = new SteamUser.LogOnDetails
            {
                Username = BotSettings.Username,
                Password = BotSettings.Password,
                SentryFileHash = BotSettings.MachineHash?.Length > 0 ? BotSettings.MachineHash : null
            };

            if (loggedOnCallback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
            {
                var mobileAuthCode = OnAuthenticatorCodeRequired().Result;

                if (string.IsNullOrEmpty(mobileAuthCode))
                {
                    BotLogger.Error("OnInternalSteamUserLoggedOn", "Bad Steam Guard mobile code.");
                    OnTerminate().Wait();
                }
                else
                {
                    BotLogger.Debug("OnInternalSteamUserLoggedOn", "Steam Guard mobile code provided for login.");
                    loginDetails.TwoFactorCode = mobileAuthCode;
                    loginDetails.SentryFileHash = null;
                }
            }
            else if (loggedOnCallback.Result == EResult.TwoFactorCodeMismatch)
            {
                SteamTime.ReAlignTime().Wait();
                var mobileAuthCode = OnAuthenticatorCodeRequired().Result;

                if (string.IsNullOrEmpty(mobileAuthCode))
                {
                    BotLogger.Error("OnInternalSteamUserLoggedOn", "Bad Steam Guard mobile code.");
                    OnTerminate().Wait();
                }
                else
                {
                    BotLogger.Debug("OnInternalSteamUserLoggedOn", "Steam Guard mobile code provided for login.");
                    loginDetails.TwoFactorCode = mobileAuthCode;
                    loginDetails.SentryFileHash = null;
                }
            }
            else if (loggedOnCallback.Result == EResult.AccountLogonDenied ||
                     loggedOnCallback.Result == EResult.InvalidLoginAuthCode)
            {
                var emailAuthCode = OnEmailCodeRequired().Result;

                if (string.IsNullOrEmpty(emailAuthCode))
                {
                    BotLogger.Error("OnInternalSteamUserLoggedOn", "Bad Steam Guard email code.");
                    OnTerminate().Wait();
                }
                else
                {
                    BotLogger.Debug("OnInternalSteamUserLoggedOn", "Steam Guard email code provided for login.");
                    loginDetails.AuthCode = emailAuthCode;
                    loginDetails.SentryFileHash = null;
                }
            }
            else if (loggedOnCallback.Result == EResult.InvalidPassword)
            {
                BotLogger.Error("OnInternalSteamUserLoggedOn", "Invalid user name and password combination.");
                OnTerminate().Wait();
            }
            else if (loggedOnCallback.Result == EResult.RateLimitExceeded)
            {
                // ignore
            }

            InternalInitializeLogin(loginDetails);
        }

        private void OnInternalSteamUserNewWebApiUserNonce(SteamUser.WebAPIUserNonceCallback webAPIUserNonceCallback)
        {
            if (webAPIUserNonceCallback.Result == EResult.OK)
            {
                var session = SteamClient.AuthenticateWebSession(WebAPI, webAPIUserNonceCallback.Nonce).Result;

                if (session != null)
                {
                    LoginBackoff.Reset();
                    OnNewWebSessionAvailable(session).Wait();
                }
            }
        }

        private void OnInternalSteamUserUpdateMachineAuthenticationCallback(
            SteamUser.UpdateMachineAuthCallback machineAuthCallback)
        {
            using (var sha = new SHA1Managed())
            {
                BotSettings.MachineHash = sha.ComputeHash(machineAuthCallback.Data);
            }

            BotSettings.SaveSettings();

            var authResponse = new SteamUser.MachineAuthDetails
            {
                BytesWritten = machineAuthCallback.BytesToWrite,
                FileName = machineAuthCallback.FileName,
                FileSize = machineAuthCallback.BytesToWrite,
                Offset = machineAuthCallback.Offset,
                SentryFileHash = BotSettings.MachineHash,
                OneTimePassword = machineAuthCallback.OneTimePassword,
                LastError = 0,
                Result = EResult.OK,
                JobID = machineAuthCallback.JobID
            };

            SteamUser.SendMachineAuthResponse(authResponse);
        }


        private void OnInternalUserNotifications(ClientUserNotificationsCallback clientUserNotificationsCallback)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ISteamKitNotificationBot notificationBot)
            {
                notificationBot.OnUserNotifications(clientUserNotificationsCallback.Notifications).Wait();
            }
        }

        private async Task SteamKitPolling(object o)
        {
            try
            {
                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    try
                    {
                        CallbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(5));
                    }
                    catch (WebException e)
                    {
                        await BotLogger.Warning("SteamKitPolling", e.Message).ConfigureAwait(false);
                        await BotLogger.Debug("SteamKitPolling", "Sleeping for 60 seconds.").ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                await BotLogger.Error("SteamKitPolling", e.Message).ConfigureAwait(false);
            }

            await OnTerminate().ConfigureAwait(false);
        }
    }
}