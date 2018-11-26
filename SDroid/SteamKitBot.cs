using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SDroid.Helpers;
using SDroid.Interfaces;
using SDroid.SteamKit;
using SDroid.SteamMobile;
using SDroid.SteamWeb;
using SteamKit2;

namespace SDroid
{
    // ReSharper disable once ClassTooBig
    public abstract class SteamKitBot : SteamBot, ISteamKitBot
    {
        protected ExponentialBackoff ConnectionBackoff = new ExponentialBackoff();
        protected ExponentialBackoff LoginBackoff = new ExponentialBackoff();
        protected SteamUser.LogOnDetails LoginDetails;
        protected Timer StalledLoginCheckTimer;
        protected List<IDisposable> SubscribedCallbacks = new List<IDisposable>();

        protected SteamKitBot(ISteamKitBotSettings settings, IBotLogger botLogger) : base(settings, botLogger)
        {
            CancellationTokenSource = new CancellationTokenSource();

            SteamClient = new SteamClient();
            CallbackManager = new CallbackManager(SteamClient);
            SteamUser = SteamClient.GetHandler<SteamUser>();
            SteamFriends = SteamClient.GetHandler<SteamFriends>();

            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnInternalSteamClientConnected));
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnInternalSteamClientDisconnect));

            SubscribedCallbacks.Add(CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnInternalSteamUserLoggedOn));
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnInternalSteamUserLoggedOff));
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnInternalSteamUserLoginKeyExchange));
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.WebAPIUserNonceCallback>(OnInternalSteamUserNewWebApiUserNonce));
            SubscribedCallbacks.Add(CallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(
                OnInternalSteamUserUpdateMachineAuthenticationCallback));
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnInternalAccountInfoAvailable));
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.WalletInfoCallback>(OnInternalWalletInfoAvailable));

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ISteamKitChatBot)
            {
                SubscribedCallbacks.Add(
                    CallbackManager.Subscribe<SteamFriends.FriendMsgCallback>(
                        OnInternalFriendSteamFriendsMessageReceived));
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            StalledLoginCheckTimer?.Dispose();

            foreach (var callback in SubscribedCallbacks)
            {
                callback?.Dispose();
            }

            SubscribedCallbacks.Clear();

            SubscribedCallbacks = null;
            SteamClient = null;
            SteamUser = null;
            SteamFriends = null;
            CallbackManager = null;
        }

        public override SteamID SteamId
        {
            get => SteamClient.SteamID ?? base.SteamId;
        }

        public new ISteamKitBotSettings BotSettings
        {
            get => base.BotSettings as ISteamKitBotSettings;
        }

        public CallbackManager CallbackManager { get; protected set; }
        public SteamClient SteamClient { get; protected set; }
        public SteamFriends SteamFriends { get; protected set; }
        public SteamUser SteamUser { get; protected set; }

        public override async Task StartBot()
        {
            lock (this)
            {
                if (BotStatus != SteamBotStatus.Ready)
                {
                    return;
                }

                BotStatus = SteamBotStatus.Connecting;
            }

            await BotLogger.Debug(nameof(StartBot), "Starting bot.").ConfigureAwait(false);

            CancellationTokenSource = new CancellationTokenSource();

            var _ = Task.Factory.StartNew(SteamKitPolling, CancellationTokenSource.Token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously);

            await BotLogger.Debug(nameof(StartBot), "Connecting to steam network.").ConfigureAwait(false);
            SteamClient.Connect();
        }

        /// <inheritdoc />
        public override async Task StopBot()
        {
            await BotLogger.Debug(nameof(StopBot), "Disconnecting from Steam network.").ConfigureAwait(false);

            SteamClient?.Disconnect();

            await base.StopBot().ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task BotLogin()
        {
            lock (this)
            {
                if (BotStatus == SteamBotStatus.LoggingIn)
                {
                    return;
                }

                BotStatus = SteamBotStatus.LoggingIn;
            }

            await InternalInitializeLogin().ConfigureAwait(false);

            while (BotStatus == SteamBotStatus.LoggingIn)
            {
                await Task.Delay(200).ConfigureAwait(false);
            }
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

                await BotLogger.Debug(nameof(OnCheckSession), "Checking session.").ConfigureAwait(false);

                if (!await WebAccess.VerifySession().ConfigureAwait(false))
                {
                    await BotLogger
                        .Warning(nameof(OnCheckSession), "Session expired. Requesting new WebAPI user nonce.")
                        .ConfigureAwait(false);

                    await SteamUser.RequestWebAPIUserNonce().ToTask().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                await BotLogger.Error(nameof(OnCheckSession), e.Message).ConfigureAwait(false);
                await OnLoggedOut().ConfigureAwait(false);
                await OnTerminate().ConfigureAwait(false);
            }
        }

        protected virtual Task OnAccountInfoAvailable(SteamUser.AccountInfoCallback accountInfo)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnConnected()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnDisconnected()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnStalledLoginCheck()
        {
            lock (this)
            {
                if (BotStatus != SteamBotStatus.LoggingIn)
                {
                    return;
                }
            }

            BotLogger.Warning(nameof(OnStalledLoginCheck), "Login stalled, trying again.");
            InternalInitializeLogin();
        }

        protected virtual Task OnWalletInfoAvailable(SteamUser.WalletInfoCallback walletInfo)
        {
            return Task.CompletedTask;
        }

        private void InternalInitializeLogin()
        {
            LoginBackoff.Delay().Wait();
            OnLoggingIn().Wait();

            lock (this)
            {
                StalledLoginCheckTimer?.Dispose();
                StalledLoginCheckTimer = new Timer(state => OnStalledLoginCheck(), null,
                    TimeSpan.FromSeconds(BotSettings.LoginTimeout), TimeSpan.FromMilliseconds(-1));
            }

            BotLogger.Debug(nameof(InternalInitializeLogin), "Starting login process.");
            LoginDetails = LoginDetails ??
                           new SteamUser.LogOnDetails
                           {
                               Username = BotSettings.Username,
                               SentryFileHash =
                                   BotSettings.SentryFileHash?.Length > 0 ? BotSettings.SentryFileHash : null,
                               ShouldRememberPassword = true,
                               LoginKey = BotSettings.LoginKey
                           };

            if (string.IsNullOrWhiteSpace(LoginDetails.Password) && string.IsNullOrWhiteSpace(LoginDetails.LoginKey))
            {
                BotLogger.Debug(nameof(InternalInitializeLogin), "Requesting account password.");
                var password = OnPasswordRequired().Result;

                if (string.IsNullOrWhiteSpace(password))
                {
                    BotLogger.Error(nameof(InternalInitializeLogin), "Bad password provided.");
                    OnTerminate().Wait();

                    return;
                }

                LoginDetails.Password = password;
            }

            SteamUser.LogOn(LoginDetails);
        }

        private void OnInternalAccountInfoAvailable(SteamUser.AccountInfoCallback accountInfoCallback)
        {
            BotLogger.Debug(nameof(OnInternalAccountInfoAvailable), "Account info available.");
            OnAccountInfoAvailable(accountInfoCallback).Wait();
        }

        private void OnInternalFriendSteamFriendsMessageReceived(SteamFriends.FriendMsgCallback friendMsgCallback)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ISteamKitChatBot chatBot)
            {
                BotLogger.Debug(nameof(OnInternalFriendSteamFriendsMessageReceived),
                    "Received a new chat event. SteamFriends.FriendMsgCallback.EntryType = `{0}`",
                    friendMsgCallback.EntryType);

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
            BotLogger.Debug(nameof(OnInternalSteamClientConnected), "Connected to the steam network.");
            ConnectionBackoff.Reset();

            lock (this)
            {
                BotStatus = SteamBotStatus.Connected;
            }

            OnConnected().Wait();
        }

        private void OnInternalSteamClientDisconnect(SteamClient.DisconnectedCallback disconnectedCallback)
        {
            BotLogger.Debug(nameof(OnInternalSteamClientDisconnect),
                "Disconnected from the steam network. SteamClient.DisconnectedCallback.UserInitiated = `{0}`",
                disconnectedCallback.UserInitiated);

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

                BotLogger.Debug(nameof(OnInternalSteamClientDisconnect), "Reconnecting to the steam network.");
                SteamClient.Connect();
            }
            else
            {
                OnDisconnected().Wait();
            }
        }

        private void OnInternalSteamUserLoggedOff(SteamUser.LoggedOffCallback loggedOffCallback)
        {
            BotLogger.Debug(nameof(OnInternalSteamUserLoggedOff), "SteamUser.LoggedOffCallback.Result = `{0}`",
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

            BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn),
                "SteamUser.LoggedOnCallback.Result = `{0}`, SteamUser.LoggedOnCallback.ExtendedResult = `{1}`",
                loggedOnCallback.Result, loggedOnCallback.ExtendedResult);

            if (loggedOnCallback.Result == EResult.OK)
            {
                BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn), "Retriving WebAPI and WebAccess session.");
                var session = SteamClient.AuthenticateWebSession(loggedOnCallback.WebAPIUserNonce).Result;

                if (session != null && session.HasEnoughInfo())
                {
                    if (new SteamWebAccess(
                            session,
                            IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress)
                                ? ipAddress
                                : IPAddress.Any,
                            string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy))
                        .VerifySession().Result)
                    {
                        BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn), "Session is valid.");
                        OnNewWebSessionAvailable(session).Wait();
                        LoginBackoff.Reset();
                    }
                    else
                    {
                        BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn),
                            "Bad session retrieved. Requesting a new WebAPI user nonce.");
                        SteamUser.RequestWebAPIUserNonce();
                    }
                }
                else
                {
                    BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn),
                        "Failed to retrieve WebAccess session. Forcefully starting a new login process.");
                    InternalInitializeLogin();
                }

                return;
            }

            BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn), "Clearing saved LoginKey and SentryFile data.");
            LoginDetails.LoginKey = null;
            LoginDetails.SentryFileHash = null;

            BotSettings.LoginKey = null;
            BotSettings.SentryFile = null;
            BotSettings.SentryFileHash = null;
            BotSettings.SentryFileName = null;
            BotSettings.SaveSettings();

            if (loggedOnCallback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
            {
                BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn), "Requesting authenticator code.");
                var mobileAuthCode = OnAuthenticatorCodeRequired().Result;

                if (string.IsNullOrEmpty(mobileAuthCode))
                {
                    BotLogger.Error(nameof(OnInternalSteamUserLoggedOn), "Bad authenticator code provided.");
                    OnTerminate().Wait();

                    return;
                }

                LoginBackoff.Reset();
                LoginDetails.TwoFactorCode = mobileAuthCode;
            }
            else if (loggedOnCallback.Result == EResult.TwoFactorCodeMismatch)
            {
                BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn), "Realigning authenticator clock.");
                SteamTime.ReAlignTime().Wait();

                BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn), "Requesting authenticator code.");
                var mobileAuthCode = OnAuthenticatorCodeRequired().Result;

                if (string.IsNullOrEmpty(mobileAuthCode))
                {
                    BotLogger.Error(nameof(OnInternalSteamUserLoggedOn), "Bad authenticator code provided.");
                    OnTerminate().Wait();

                    return;
                }

                LoginBackoff.Reset();
                LoginDetails.TwoFactorCode = mobileAuthCode;
            }
            else if (loggedOnCallback.Result == EResult.AccountLogonDenied ||
                     loggedOnCallback.Result == EResult.InvalidLoginAuthCode)
            {
                BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn), "Requesting email verification code.");
                var emailAuthCode = OnEmailCodeRequired().Result;

                if (string.IsNullOrEmpty(emailAuthCode))
                {
                    BotLogger.Error(nameof(OnInternalSteamUserLoggedOn), "Bad email verification code provided.");
                    OnTerminate().Wait();

                    return;
                }

                LoginBackoff.Reset();
                LoginDetails.AuthCode = emailAuthCode;
            }
            else if (loggedOnCallback.Result == EResult.InvalidPassword)
            {
                BotLogger.Debug(nameof(OnInternalSteamUserLoggedOn), "Requesting account password.");
                var password = OnPasswordRequired().Result;

                if (string.IsNullOrWhiteSpace(password))
                {
                    BotLogger.Error(nameof(OnInternalSteamUserLoggedOn), "Bad password provided.");
                    OnTerminate().Wait();

                    return;
                }

                LoginBackoff.Reset();
                LoginDetails.Password = password;
            }
            else if (loggedOnCallback.Result == EResult.RateLimitExceeded)
            {
                // ignore
            }

            InternalInitializeLogin();
        }

        private void OnInternalSteamUserLoginKeyExchange(SteamUser.LoginKeyCallback loginKeyCallback)
        {
            if (!string.IsNullOrWhiteSpace(loginKeyCallback.LoginKey))
            {
                BotSettings.LoginKey = loginKeyCallback.LoginKey;
                BotSettings.SaveSettings();

                SteamUser.AcceptNewLoginKey(loginKeyCallback);

                BotLogger.Debug(nameof(OnInternalSteamUserLoginKeyExchange),
                    "Login key exchange completed.");
            }
        }

        private void OnInternalSteamUserNewWebApiUserNonce(SteamUser.WebAPIUserNonceCallback webAPIUserNonceCallback)
        {
            if (webAPIUserNonceCallback.Result == EResult.OK)
            {
                BotLogger.Debug(nameof(OnInternalSteamUserNewWebApiUserNonce),
                    "New WebAPI web nonce received. Retriving WebAPI and WebAccess session.");
                var session = SteamClient.AuthenticateWebSession(webAPIUserNonceCallback.Nonce).Result;

                if (session != null && session.HasEnoughInfo())
                {
                    if (new SteamWebAccess(
                            session,
                            IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress)
                                ? ipAddress
                                : IPAddress.Any,
                            string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy))
                        .VerifySession().Result)
                    {
                        BotLogger.Debug(nameof(OnInternalSteamUserNewWebApiUserNonce), "Session is valid.");
                        LoginBackoff.Reset();
                        OnNewWebSessionAvailable(session).Wait();
                    }
                    else
                    {
                        BotLogger.Debug(nameof(OnInternalSteamUserNewWebApiUserNonce),
                            "Session is invalid. Requesting a new WebAPI user nonce.");
                        SteamUser.RequestWebAPIUserNonce();
                    }
                }
                else
                {
                    BotLogger.Debug(nameof(OnInternalSteamUserNewWebApiUserNonce),
                        "Failed to retrieve WebAccess session.");
                }
            }
        }

        private void OnInternalSteamUserUpdateMachineAuthenticationCallback(
            SteamUser.UpdateMachineAuthCallback machineAuthCallback)
        {
            BotLogger.Debug(nameof(OnInternalSteamUserUpdateMachineAuthenticationCallback),
                "Machine authentication SentryFile update request received. SteamUser.UpdateMachineAuthCallback.FileName = `{0}`",
                machineAuthCallback.FileName);

            var sentryFile = new byte[0];

            if (machineAuthCallback.FileName == BotSettings.SentryFileName && BotSettings.SentryFile != null)
            {
                sentryFile = BotSettings.SentryFile;
            }

            Array.Resize(ref sentryFile,
                Math.Max(sentryFile.Length,
                    Math.Min(machineAuthCallback.BytesToWrite, machineAuthCallback.Data.Length) +
                    machineAuthCallback.Offset));

            Array.Copy(machineAuthCallback.Data, 0, sentryFile, machineAuthCallback.Offset,
                Math.Min(machineAuthCallback.BytesToWrite, machineAuthCallback.Data.Length));

            byte[] sentryFileHash;

            using (var sha = new SHA1Managed())
            {
                sentryFileHash = sha.ComputeHash(sentryFile);
            }

            var authResponse = new SteamUser.MachineAuthDetails
            {
                BytesWritten = machineAuthCallback.BytesToWrite,
                FileName = machineAuthCallback.FileName,
                FileSize = sentryFile.Length,
                Offset = machineAuthCallback.Offset,
                SentryFileHash = sentryFileHash,
                OneTimePassword = machineAuthCallback.OneTimePassword,
                LastError = 0,
                Result = EResult.OK,
                JobID = machineAuthCallback.JobID
            };

            SteamUser.SendMachineAuthResponse(authResponse);

            BotSettings.SentryFileName = machineAuthCallback.FileName;
            BotSettings.SentryFileHash = sentryFileHash;
            BotSettings.SentryFile = sentryFile;
            BotSettings.SaveSettings();

            BotLogger.Debug(nameof(OnInternalSteamUserUpdateMachineAuthenticationCallback), "SentryFile updated.");
        }

        private void OnInternalWalletInfoAvailable(SteamUser.WalletInfoCallback walletInfoCallback)
        {
            BotLogger.Debug(nameof(OnInternalWalletInfoAvailable), "Account wallet information available.");
            OnWalletInfoAvailable(walletInfoCallback).Wait();
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
                        await BotLogger.Warning(nameof(SteamKitPolling), e.Message).ConfigureAwait(false);
                        await BotLogger.Debug(nameof(SteamKitPolling), "Sleeping for 60 seconds.")
                            .ConfigureAwait(false);
                        await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                await BotLogger.Error(nameof(SteamKitPolling), e.Message).ConfigureAwait(false);
            }

            await OnTerminate().ConfigureAwait(false);
        }
    }
}