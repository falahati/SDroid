using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        // ReSharper disable once SuggestBaseTypeForParameter
        protected SteamKitBot(ISteamKitBotSettings settings, ILogger botLogger) : base(settings, botLogger)
        {
            CancellationTokenSource = new CancellationTokenSource();
            SteamClient = new SteamClient(
                SteamConfiguration.Create(
                    builder =>
                    {
                        if (settings.ConnectionTimeout != null)
                        {
                            builder.WithConnectionTimeout(
                                TimeSpan.FromSeconds(settings.ConnectionTimeout.Value)
                            );
                        }

                        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                        {
                            builder.WithWebAPIKey(settings.ApiKey);
                        }
                    }
                )
            );
            CallbackManager = new CallbackManager(SteamClient);
            SteamUser = SteamClient.GetHandler<SteamUser>();
            SteamFriends = SteamClient.GetHandler<SteamFriends>();

            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnInternalSteamClientConnected)
            );
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnInternalSteamClientDisconnect)
            );
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnInternalSteamUserLoggedOn)
            );
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnInternalSteamUserLoggedOff)
            );
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnInternalSteamUserLoginKeyExchange)
            );
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.WebAPIUserNonceCallback>(OnInternalSteamUserNewWebApiUserNonce)
            );
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnInternalSteamUserUpdateMachineAuthenticationCallback)
            );
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.AccountInfoCallback>(OnInternalAccountInfoAvailable)
            );
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamUser.WalletInfoCallback>(OnInternalWalletInfoAvailable)
            );
        }

        public new ISteamKitBotSettings BotSettings
        {
            get => base.BotSettings as ISteamKitBotSettings;
        }

        public CallbackManager CallbackManager { get; set; }

        public SteamClient SteamClient { get; set; }

        public SteamFriends SteamFriends { get; set; }

        protected override SteamID SteamId
        {
            get => SteamClient?.SteamID ?? base.SteamId;
        }

        protected SteamUser SteamUser { get; set; }

        ISteamKitBotSettings ISteamKitBot.BotSettings
        {
            get => BotSettings;
        }

        CallbackManager ISteamKitBot.CallbackManager
        {
            get => CallbackManager;
        }

        SteamClient ISteamKitBot.SteamClient
        {
            get => SteamClient;
        }

        SteamFriends ISteamKitBot.SteamFriends
        {
            get => SteamFriends;
        }

        SteamUser ISteamKitBot.SteamUser
        {
            get => SteamUser;
        }

        public override Task StartBot()
        {
            lock (LocalLock)
            {
                if (BotStatus != SteamBotStatus.Ready)
                {
                    return Task.CompletedTask;
                }

                BotStatus = SteamBotStatus.Connecting;
            }

            BotLogger.LogInformation("[{0}] Starting bot...", SteamId?.ConvertToUInt64());

            CancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(
                SteamKitPolling,
                CancellationTokenSource.Token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously
            );

            BotLogger.LogDebug("[{0}] Connecting to steam network...", SteamId?.ConvertToUInt64());
            SteamClient.Connect();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override async Task StopBot()
        {
            lock (LocalLock)
            {
                StalledLoginCheckTimer?.Dispose();
            }
            BotLogger.LogDebug("[{0}] Disconnecting from Steam network...", SteamId?.ConvertToUInt64());
            SteamClient?.Disconnect();
            await base.StopBot().ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override async Task BotLogin()
        {
            lock (LocalLock)
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

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

        protected override async Task OnCheckSession()
        {
            try
            {
                lock (LocalLock)
                {
                    if (BotStatus != SteamBotStatus.Running || WebAccess == null)
                    {
                        return;
                    }
                }

                BotLogger.LogDebug("[{0}] Checking session...", SteamId?.ConvertToUInt64());

                if (await WebAccess.VerifySession().ConfigureAwait(false))
                {
                    return;
                }

                BotLogger.LogDebug("[{0}] Session expired. Requesting new WebAPI user nonce.", SteamId?.ConvertToUInt64());
                await SteamUser.RequestWebAPIUserNonce().ToTask().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, "[{0}] {1}", SteamId?.ConvertToUInt64(), e.Message);
                lock (LocalLock)
                {
                    StalledLoginCheckTimer?.Dispose();
                }
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
            lock (LocalLock)
            {
                if (BotStatus != SteamBotStatus.LoggingIn)
                {
                    return;
                }
            }

            BotLogger.LogWarning("[{0}] Login stalled, trying again.", SteamId?.ConvertToUInt64());
            InternalInitializeLogin().Wait();
        }

        protected virtual Task OnWalletInfoAvailable(SteamUser.WalletInfoCallback walletInfo)
        {
            return Task.CompletedTask;
        }

        private async Task InternalInitializeLogin()
        {
            await LoginBackoff.Delay().ConfigureAwait(false);
            await OnLoggingIn().ConfigureAwait(false);

            lock (LocalLock)
            {
                StalledLoginCheckTimer?.Dispose();
                StalledLoginCheckTimer = new Timer(
                    state => OnStalledLoginCheck(),
                    null,
                    TimeSpan.FromSeconds(BotSettings.LoginTimeout),
                    TimeSpan.FromMilliseconds(-1)
                );
            }

            try
            {
                BotLogger.LogInformation("[{0}] Starting login process...", SteamId?.ConvertToUInt64());
                LoginDetails = LoginDetails ??
                               new SteamUser.LogOnDetails
                               {
                                   Username = BotSettings.Username,
                                   SentryFileHash = BotSettings.SentryFileHash?.Length > 0 ? BotSettings.SentryFileHash : null,
                                   ShouldRememberPassword = true,
                                   LoginKey = BotSettings.LoginKey
                               };

                if (string.IsNullOrWhiteSpace(LoginDetails.Password) && string.IsNullOrWhiteSpace(LoginDetails.LoginKey))
                {
                    BotLogger.LogDebug("[{0}] Requesting account password.", SteamId?.ConvertToUInt64());
                    var password = await OnPasswordRequired();

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        BotLogger.LogError("[{0}] Bad password provided.", SteamId?.ConvertToUInt64());
                        lock (LocalLock)
                        {
                            StalledLoginCheckTimer?.Dispose();
                        }
                        await OnTerminate().ConfigureAwait(false);

                        return;
                    }

                    LoginDetails.Password = password;
                }

                SteamUser.LogOn(LoginDetails);
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, "[{0}] Requesting account password.", SteamId?.ConvertToUInt64());
            }
        }

        private void OnInternalAccountInfoAvailable(SteamUser.AccountInfoCallback accountInfoCallback)
        {
            BotLogger.LogTrace("[{0}] Account info available. Flags = `{1:F}`", SteamId?.ConvertToUInt64(), accountInfoCallback.AccountFlags);
            OnAccountInfoAvailable(accountInfoCallback);
        }


        private void OnInternalSteamClientConnected(SteamClient.ConnectedCallback connectedCallback)
        {
            BotLogger.LogInformation(
                "[{0}] Connected to the steam network. Endpoint = `{1}`", 
                SteamId?.ConvertToUInt64(), 
                SteamClient.CurrentEndPoint
            );
            ConnectionBackoff.Reset();

            lock (LocalLock)
            {
                BotStatus = SteamBotStatus.Connected;
            }

            OnConnected();
        }

        private void OnInternalSteamClientDisconnect(SteamClient.DisconnectedCallback disconnectedCallback)
        {
            BotLogger.LogTrace(
                "[{0}] Disconnected from the steam network. Endpoint = `{1}`, UserInitiated = `{2}`",
                SteamId?.ConvertToUInt64(),
                SteamClient.CurrentEndPoint,
                disconnectedCallback.UserInitiated
            );

            if (!disconnectedCallback.UserInitiated)
            {
                lock (LocalLock)
                {
                    if (BotStatus == SteamBotStatus.Faulted)
                    {
                        return;
                    }

                    BotStatus = SteamBotStatus.Connecting;
                }

                OnDisconnected();
                ConnectionBackoff.Delay().Wait();

                BotLogger.LogInformation("[{0}] Reconnecting to the steam network...", SteamId?.ConvertToUInt64());
                SteamClient.Connect();
            }
            else
            {
                OnDisconnected();
            }
        }

        private void OnInternalSteamUserLoggedOff(SteamUser.LoggedOffCallback loggedOffCallback)
        {
            BotLogger.LogTrace(
                "[{0}] SteamUser.LoggedOffCallback.Result = `{1}`",
                SteamId?.ConvertToUInt64(),
                loggedOffCallback.Result
            );

            lock (LocalLock)
            {
                if (BotStatus == SteamBotStatus.Faulted)
                {
                    return;
                }
            }

            if (
                EResult.Invalid == loggedOffCallback.Result ||
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
                EResult.AccountLoginDeniedThrottle == loggedOffCallback.Result
            )
            {
                InternalInitializeLogin().Wait();
            }
            else
            {
                OnLoggedOut();
            }
        }

        // ReSharper disable once MethodTooLong
        // ReSharper disable once ExcessiveIndentation
        private void OnInternalSteamUserLoggedOn(SteamUser.LoggedOnCallback loggedOnCallback)
        {
            lock (LocalLock)
            {
                if (BotStatus != SteamBotStatus.LoggingIn)
                {
                    return;
                }
            }

            BotLogger.LogTrace(
                "[{0}] SteamUser.LoggedOnCallback.Result = `{1}`, SteamUser.LoggedOnCallback.ExtendedResult = `{2}`",
                SteamId?.ConvertToUInt64(),
                loggedOnCallback.Result,
                loggedOnCallback.ExtendedResult
            );

            if (loggedOnCallback.Result == EResult.OK)
            {
                lock (LocalLock)
                {
                    StalledLoginCheckTimer?.Dispose();
                }

                BotLogger.LogTrace("[{0}] Retriving WebAPI and WebAccess session.", SteamId?.ConvertToUInt64());
                var session = SteamClient.AuthenticateWebSession(loggedOnCallback.WebAPIUserNonce).Result;

                if (session != null && session.HasEnoughInfo())
                {
                    if (
                        new SteamWebAccess(
                            session,
                            IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress)
                                ? ipAddress
                                : IPAddress.Any,
                            string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy)
                        ).VerifySession().Result
                    )
                    {
                        BotLogger.LogTrace("[{0}] Session is valid.", SteamId?.ConvertToUInt64());
                        OnNewWebSessionAvailable(session).Wait();
                        LoginBackoff.Reset();
                    }
                    else
                    {
                        BotLogger.LogDebug("[{0}] Bad session retrieved. Requesting a new WebAPI user nonce.", SteamId?.ConvertToUInt64());
                        SteamUser.RequestWebAPIUserNonce();
                    }
                }
                else
                {
                    BotLogger.LogDebug("[{0}] Failed to retrieve WebAccess session. Trying to recover session.", SteamId?.ConvertToUInt64());

                    if (RecoverWebSession().Result)
                    {
                        LoginBackoff.Reset();
                    }
                    else
                    {
                        BotLogger.LogDebug("[{0}] Forcefully starting a new login process.", SteamId?.ConvertToUInt64());
                        InternalInitializeLogin().Wait();
                    }
                }

                return;
            }

            BotLogger.LogTrace("[{0}] Clearing saved LoginKey and SentryFile data.", SteamId?.ConvertToUInt64());
            LoginDetails.LoginKey = null;
            LoginDetails.SentryFileHash = null;

            BotSettings.LoginKey = null;
            BotSettings.SentryFile = null;
            BotSettings.SentryFileHash = null;
            BotSettings.SentryFileName = null;
            BotSettings.SaveSettings();

            if (loggedOnCallback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
            {
                BotLogger.LogDebug("[{0}] Requesting authenticator code.", SteamId?.ConvertToUInt64());
                var mobileAuthCode = OnAuthenticatorCodeRequired().Result;

                if (string.IsNullOrEmpty(mobileAuthCode))
                {
                    BotLogger.LogError("[{0}] Bad authenticator code provided.", SteamId?.ConvertToUInt64());
                    lock (LocalLock)
                    {
                        StalledLoginCheckTimer?.Dispose();
                    }
                    OnTerminate().Wait();

                    return;
                }

                LoginBackoff.Reset();
                LoginDetails.TwoFactorCode = mobileAuthCode;
            }
            else if (loggedOnCallback.Result == EResult.TwoFactorCodeMismatch)
            {
                BotLogger.LogTrace("[{0}] Realigning authenticator clock.", SteamId?.ConvertToUInt64());
                SteamTime.ReAlignTime().Wait();

                BotLogger.LogDebug("[{0}] Requesting authenticator code.", SteamId?.ConvertToUInt64());
                var mobileAuthCode = OnAuthenticatorCodeRequired().Result;

                if (string.IsNullOrEmpty(mobileAuthCode))
                {
                    BotLogger.LogError("[{0}] Bad authenticator code provided.", SteamId?.ConvertToUInt64());
                    lock (LocalLock)
                    {
                        StalledLoginCheckTimer?.Dispose();
                    }
                    OnTerminate().Wait();

                    return;
                }

                // LoginBackoff.Reset(); allow backoff
                LoginDetails.TwoFactorCode = mobileAuthCode;
            }
            else if (
                loggedOnCallback.Result == EResult.AccountLogonDenied ||
                loggedOnCallback.Result == EResult.InvalidLoginAuthCode
            )
            {
                BotLogger.LogDebug("[{0}] Requesting email verification code.", SteamId?.ConvertToUInt64());
                var emailAuthCode = OnEmailCodeRequired().Result;

                if (string.IsNullOrEmpty(emailAuthCode))
                {
                    BotLogger.LogError("[{0}] Bad email verification code provided.", SteamId?.ConvertToUInt64());
                    lock (LocalLock)
                    {
                        StalledLoginCheckTimer?.Dispose();
                    }
                    OnTerminate().Wait();

                    return;
                }

                // LoginBackoff.Reset(); allow backoff
                LoginDetails.AuthCode = emailAuthCode;
            }
            else if (loggedOnCallback.Result == EResult.InvalidPassword)
            {
                BotLogger.LogDebug("[{0}] Requesting account password.", SteamId?.ConvertToUInt64());
                var password = OnPasswordRequired().Result;

                if (string.IsNullOrWhiteSpace(password))
                {
                    BotLogger.LogError("[{0}] Bad password provided.", SteamId?.ConvertToUInt64());
                    lock (LocalLock)
                    {
                        StalledLoginCheckTimer?.Dispose();
                    }
                    OnTerminate().Wait();

                    return;
                }

                // LoginBackoff.Reset(); allow backoff
                LoginDetails.Password = password;
            }
            else if (loggedOnCallback.Result == EResult.ServiceUnavailable || loggedOnCallback.Result == EResult.TryAnotherCM)
            {
                lock (LocalLock)
                {
                    StalledLoginCheckTimer?.Dispose();
                }
                OnTerminate().Wait();
                return;
            }
            else if (loggedOnCallback.Result == EResult.RateLimitExceeded)
            {
                // ignore
            }

            InternalInitializeLogin().Wait();
        }

        private void OnInternalSteamUserLoginKeyExchange(SteamUser.LoginKeyCallback loginKeyCallback)
        {
            if (string.IsNullOrWhiteSpace(loginKeyCallback.LoginKey))
            {
                return;
            }

            BotSettings.LoginKey = loginKeyCallback.LoginKey;
            BotSettings.SaveSettings();

            SteamUser.AcceptNewLoginKey(loginKeyCallback);

            BotLogger.LogInformation("[{0}] Login key exchange completed.", SteamId?.ConvertToUInt64());
        }

        private void OnInternalSteamUserNewWebApiUserNonce(SteamUser.WebAPIUserNonceCallback webAPIUserNonceCallback)
        {
            if (webAPIUserNonceCallback.Result == EResult.OK)
            {
                BotLogger.LogTrace("[{0}] New WebAPI web nonce received. Retriving WebAPI and WebAccess session.", SteamId?.ConvertToUInt64());
                var session = SteamClient.AuthenticateWebSession(webAPIUserNonceCallback.Nonce).Result;

                if (session != null && session.HasEnoughInfo())
                {
                    if (
                        new SteamWebAccess(
                                session,
                                IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress)
                                    ? ipAddress
                                    : IPAddress.Any,
                                string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy)
                            )
                            .VerifySession().Result
                    )
                    {
                        BotLogger.LogTrace("[{0}] Session is valid.", SteamId?.ConvertToUInt64());
                        LoginBackoff.Reset();
                        OnNewWebSessionAvailable(session).Wait();
                    }
                    else
                    {
                        BotLogger.LogDebug("[{0}] Session is invalid. Requesting a new WebAPI user nonce.", SteamId?.ConvertToUInt64());
                        SteamUser.RequestWebAPIUserNonce();
                    }
                }
                else
                {
                    BotLogger.LogWarning("[{0}] Failed to retrieve WebAccess session.", SteamId?.ConvertToUInt64());
                }
            }
        }

        private void OnInternalSteamUserUpdateMachineAuthenticationCallback(
            SteamUser.UpdateMachineAuthCallback machineAuthCallback)
        {
            BotLogger.LogTrace(
                "[{0}] Machine authentication SentryFile update request received. SteamUser.UpdateMachineAuthCallback.FileName = `{1}`",
                SteamId?.ConvertToUInt64(),
                machineAuthCallback.FileName
            );

            var sentryFile = new byte[0];

            if (machineAuthCallback.FileName == BotSettings.SentryFileName && BotSettings.SentryFile != null)
            {
                sentryFile = BotSettings.SentryFile;
            }

            Array.Resize(
                ref sentryFile,
                Math.Max(
                    sentryFile.Length,
                    Math.Min(machineAuthCallback.BytesToWrite, machineAuthCallback.Data.Length) + machineAuthCallback.Offset
                )
            );

            Array.Copy(
                machineAuthCallback.Data,
                0,
                sentryFile,
                machineAuthCallback.Offset,
                Math.Min(machineAuthCallback.BytesToWrite, machineAuthCallback.Data.Length)
            );

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

            BotLogger.LogDebug("[{0}] SentryFile updated.", SteamId?.ConvertToUInt64());
        }

        private void OnInternalWalletInfoAvailable(SteamUser.WalletInfoCallback walletInfoCallback)
        {
            BotLogger.LogTrace(
                "[{0}] Account wallet information available. Balance = `{1:F2} {2:G}`", 
                SteamId?.ConvertToUInt64(), 
                walletInfoCallback.Balance / 100,
                walletInfoCallback.Currency
            );
            OnWalletInfoAvailable(walletInfoCallback);
        }

        private async Task<bool> RecoverWebSession()
        {
            try
            {
                // Check if the current session is still valid
                if (WebAccess != null)
                {
                    BotLogger.LogTrace("[{0}] Trying current session.", SteamId?.ConvertToUInt64());

                    if (await WebAccess.VerifySession().ConfigureAwait(false))
                    {
                        BotLogger.LogTrace("[{0}] Session is valid.", SteamId?.ConvertToUInt64());
                        await OnNewWebSessionAvailable(WebAccess.Session).ConfigureAwait(false);

                        return true;
                    }
                }

                // Check if the bot's authenticator holds a valid session or a session that can be extended
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is IAuthenticatorBot authenticatorController)
                {
                    BotLogger.LogTrace("[{0}] Trying authenticator session.", SteamId?.ConvertToUInt64());

                    if (authenticatorController.BotAuthenticatorSettings?.Authenticator?.Session != null)
                    {
                        var webAccess = new SteamMobileWebAccess(
                            authenticatorController.BotAuthenticatorSettings.Authenticator.Session,
                            IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress)
                                ? ipAddress
                                : IPAddress.Any,
                            string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy)
                        );

                        BotLogger.LogTrace("[{0}] Refreshing authenticator session.", SteamId?.ConvertToUInt64());

                        if (
                            await authenticatorController.BotAuthenticatorSettings.Authenticator.Session
                                .RefreshSession(webAccess).ConfigureAwait(false)
                        )
                        {
                            if (await webAccess.VerifySession().ConfigureAwait(false))
                            {
                                BotLogger.LogTrace("[{0}] Session is valid.", SteamId?.ConvertToUInt64());
                                await OnNewWebSessionAvailable(webAccess.Session).ConfigureAwait(false);

                                return true;
                            }
                        }
                    }
                }

                if (BotSettings.Session != null && BotSettings.Session.HasEnoughInfo() && WebAccess == null)
                {
                    if (this is IAuthenticatorBot)
                    {
                        BotLogger.LogTrace("[{0}] Trying last saved session as authenticator session.", SteamId?.ConvertToUInt64());

                        var webAccess = new SteamMobileWebAccess(
                            BotSettings.Session is MobileSession mobileSession ? mobileSession : new MobileSession(
                                BotSettings.Session.RememberLoginToken,
                                BotSettings.Session.SteamId,
                                BotSettings.Session.SteamLogin,
                                BotSettings.Session.SteamLoginSecure,
                                BotSettings.Session.SessionId,
                                BotSettings.Session.RememberLoginToken,
                                BotSettings.Session.SteamMachineAuthenticationTokens
                            ),
                            IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress) ? ipAddress : IPAddress.Any,
                            string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy)
                        );

                        if (await webAccess.VerifySession().ConfigureAwait(false))
                        {
                            BotLogger.LogTrace("[{0}] Session is valid.", SteamId?.ConvertToUInt64());
                            await OnNewWebSessionAvailable(webAccess.Session).ConfigureAwait(false);

                            return true;
                        }
                    }
                    else
                    {
                        BotLogger.LogTrace("[{0}] Trying last saved session.", SteamId?.ConvertToUInt64());

                        var webAccess = new SteamWebAccess(
                            BotSettings.Session,
                            IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress) ? ipAddress : IPAddress.Any,
                            string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy)
                        );

                        if (await webAccess.VerifySession().ConfigureAwait(false))
                        {
                            BotLogger.LogTrace("[{0}] Session is valid.", SteamId?.ConvertToUInt64());
                            await OnNewWebSessionAvailable(webAccess.Session).ConfigureAwait(false);

                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                BotLogger.LogWarning(e, "[{0}] Failed to recover WebSession, Error: {1}", SteamId?.ConvertToUInt64(), e.Message);
            }

            return false;
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
                        BotLogger.LogWarning(e, "[{0}] {1}", SteamId?.ConvertToUInt64(), e.Message);
                        BotLogger.LogDebug("[{0}] Sleeping for 60 seconds.", SteamId?.ConvertToUInt64());
                        await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
                    }
                }

                return;
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, "[{0}] {1}", SteamId?.ConvertToUInt64(), e.Message);
            }

            lock (LocalLock)
            {
                StalledLoginCheckTimer?.Dispose();
            }
            await OnTerminate().ConfigureAwait(false);
        }
    }
}