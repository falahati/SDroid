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

            SteamClient = new SteamClient();
            CallbackManager = new CallbackManager(SteamClient);
            SteamUser = SteamClient.GetHandler<SteamUser>();
            SteamFriends = SteamClient.GetHandler<SteamFriends>();

            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamClient.ConnectedCallback>(OnInternalSteamClientConnected)
            );
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnInternalSteamClientDisconnect)
            );

            SubscribedCallbacks.Add(CallbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnInternalSteamUserLoggedOn)
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
            SubscribedCallbacks.Add(CallbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(
                OnInternalSteamUserUpdateMachineAuthenticationCallback)
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
            get => SteamClient.SteamID ?? base.SteamId;
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

            BotLogger.LogInformation("Starting bot...");

            CancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(
                SteamKitPolling,
                CancellationTokenSource.Token,
                TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously
            );

            BotLogger.LogDebug("Connecting to steam network...");
            SteamClient.Connect();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override async Task StopBot()
        {
            BotLogger.LogDebug("Disconnecting from Steam network...");
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

                BotLogger.LogDebug("Checking session...");

                if (!await WebAccess.VerifySession().ConfigureAwait(false))
                {
                    BotLogger.LogDebug("Session expired. Requesting new WebAPI user nonce.");
                    await SteamUser.RequestWebAPIUserNonce().ToTask().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, e.Message);
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

            BotLogger.LogWarning("Login stalled, trying again.");
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
                BotLogger.LogInformation("Starting login process...");
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
                    BotLogger.LogDebug("Requesting account password.");
                    var password = await OnPasswordRequired();

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        BotLogger.LogError("Bad password provided.");
                        await OnTerminate().ConfigureAwait(false);

                        return;
                    }

                    LoginDetails.Password = password;
                }

                SteamUser.LogOn(LoginDetails);
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, "Requesting account password.");
            }
        }

        private void OnInternalAccountInfoAvailable(SteamUser.AccountInfoCallback accountInfoCallback)
        {
            BotLogger.LogTrace("Account info available.");
            OnAccountInfoAvailable(accountInfoCallback);
        }


        private void OnInternalSteamClientConnected(SteamClient.ConnectedCallback connectedCallback)
        {
            BotLogger.LogInformation("Connected to the steam network.");
            ConnectionBackoff.Reset();

            lock (LocalLock)
            {
                BotStatus = SteamBotStatus.Connected;
            }

            OnConnected();
        }

        private void OnInternalSteamClientDisconnect(SteamClient.DisconnectedCallback disconnectedCallback)
        {
            BotLogger.LogTrace("Disconnected from the steam network. SteamClient.DisconnectedCallback.UserInitiated = `{0}`");

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

                BotLogger.LogInformation("Reconnecting to the steam network...");
                SteamClient.Connect();
            }
            else
            {
                OnDisconnected();
            }
        }

        private void OnInternalSteamUserLoggedOff(SteamUser.LoggedOffCallback loggedOffCallback)
        {
            BotLogger.LogTrace("SteamUser.LoggedOffCallback.Result = `{0}`");

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
                "SteamUser.LoggedOnCallback.Result = `{0}`, SteamUser.LoggedOnCallback.ExtendedResult = `{1}`",
                loggedOnCallback.Result,
                loggedOnCallback.ExtendedResult
            );

            if (loggedOnCallback.Result == EResult.OK)
            {
                BotLogger.LogTrace("Retriving WebAPI and WebAccess session.");
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
                        BotLogger.LogTrace("Session is valid.");
                        OnNewWebSessionAvailable(session).Wait();
                        LoginBackoff.Reset();
                    }
                    else
                    {
                        BotLogger.LogDebug("Bad session retrieved. Requesting a new WebAPI user nonce.");
                        SteamUser.RequestWebAPIUserNonce();
                    }
                }
                else
                {
                    BotLogger.LogDebug("Failed to retrieve WebAccess session. Trying to recover session.");

                    if (RecoverWebSession().Result)
                    {
                        LoginBackoff.Reset();
                    }
                    else
                    {
                        BotLogger.LogDebug("Forcefully starting a new login process.");
                        InternalInitializeLogin().Wait();
                    }
                }

                return;
            }

            BotLogger.LogTrace("Clearing saved LoginKey and SentryFile data.");
            LoginDetails.LoginKey = null;
            LoginDetails.SentryFileHash = null;

            BotSettings.LoginKey = null;
            BotSettings.SentryFile = null;
            BotSettings.SentryFileHash = null;
            BotSettings.SentryFileName = null;
            BotSettings.SaveSettings();

            if (loggedOnCallback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
            {
                BotLogger.LogDebug("Requesting authenticator code.");
                var mobileAuthCode = OnAuthenticatorCodeRequired().Result;

                if (string.IsNullOrEmpty(mobileAuthCode))
                {
                    BotLogger.LogError("Bad authenticator code provided.");
                    OnTerminate().Wait();

                    return;
                }

                LoginBackoff.Reset();
                LoginDetails.TwoFactorCode = mobileAuthCode;
            }
            else if (loggedOnCallback.Result == EResult.TwoFactorCodeMismatch)
            {
                BotLogger.LogTrace("Realigning authenticator clock.");
                SteamTime.ReAlignTime().Wait();

                BotLogger.LogDebug("Requesting authenticator code.");
                var mobileAuthCode = OnAuthenticatorCodeRequired().Result;

                if (string.IsNullOrEmpty(mobileAuthCode))
                {
                    BotLogger.LogError("Bad authenticator code provided.");
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
                BotLogger.LogDebug("Requesting email verification code.");
                var emailAuthCode = OnEmailCodeRequired().Result;

                if (string.IsNullOrEmpty(emailAuthCode))
                {
                    BotLogger.LogError("Bad email verification code provided.");
                    OnTerminate().Wait();

                    return;
                }

                // LoginBackoff.Reset(); allow backoff
                LoginDetails.AuthCode = emailAuthCode;
            }
            else if (loggedOnCallback.Result == EResult.InvalidPassword)
            {
                BotLogger.LogDebug("Requesting account password.");
                var password = OnPasswordRequired().Result;

                if (string.IsNullOrWhiteSpace(password))
                {
                    BotLogger.LogError("Bad password provided.");
                    OnTerminate().Wait();

                    return;
                }

                // LoginBackoff.Reset(); allow backoff
                LoginDetails.Password = password;
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

            BotLogger.LogInformation("Login key exchange completed.");
        }

        private void OnInternalSteamUserNewWebApiUserNonce(SteamUser.WebAPIUserNonceCallback webAPIUserNonceCallback)
        {
            if (webAPIUserNonceCallback.Result == EResult.OK)
            {
                BotLogger.LogTrace("New WebAPI web nonce received. Retriving WebAPI and WebAccess session.");
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
                        BotLogger.LogTrace("Session is valid.");
                        LoginBackoff.Reset();
                        OnNewWebSessionAvailable(session).Wait();
                    }
                    else
                    {
                        BotLogger.LogDebug("Session is invalid. Requesting a new WebAPI user nonce.");
                        SteamUser.RequestWebAPIUserNonce();
                    }
                }
                else
                {
                    BotLogger.LogWarning("Failed to retrieve WebAccess session.");
                }
            }
        }

        private void OnInternalSteamUserUpdateMachineAuthenticationCallback(
            SteamUser.UpdateMachineAuthCallback machineAuthCallback)
        {
            BotLogger.LogTrace(
                "Machine authentication SentryFile update request received. SteamUser.UpdateMachineAuthCallback.FileName = `{0}`",
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

            BotLogger.LogDebug("SentryFile updated.");
        }

        private void OnInternalWalletInfoAvailable(SteamUser.WalletInfoCallback walletInfoCallback)
        {
            BotLogger.LogTrace("Account wallet information available.");
            OnWalletInfoAvailable(walletInfoCallback);
        }

        private async Task<bool> RecoverWebSession()
        {
            try
            {
                // Check if the current session is still valid
                if (WebAccess != null)
                {
                    BotLogger.LogTrace("Trying current session.");

                    if (await WebAccess.VerifySession().ConfigureAwait(false))
                    {
                        BotLogger.LogTrace("Session is valid.");
                        await OnNewWebSessionAvailable(WebAccess.Session).ConfigureAwait(false);

                        return true;
                    }
                }

                // Check if the bot's authenticator holds a valid session or a session that can be extended
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is IAuthenticatorBot authenticatorController)
                {
                    BotLogger.LogTrace("Trying authenticator session.");

                    if (authenticatorController.BotAuthenticatorSettings?.Authenticator?.Session != null)
                    {
                        var webAccess = new SteamMobileWebAccess(
                            authenticatorController.BotAuthenticatorSettings.Authenticator.Session,
                            IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress)
                                ? ipAddress
                                : IPAddress.Any,
                            string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy)
                        );

                        BotLogger.LogTrace("Refreshing authenticator session.");

                        if (
                            await authenticatorController.BotAuthenticatorSettings.Authenticator.Session
                                .RefreshSession(webAccess).ConfigureAwait(false)
                        )
                        {
                            if (await webAccess.VerifySession().ConfigureAwait(false))
                            {
                                BotLogger.LogTrace("Session is valid.");
                                WebAccess = webAccess;
                                await OnNewWebSessionAvailable(WebAccess.Session).ConfigureAwait(false);

                                return true;
                            }
                        }
                    }
                }

                if (BotSettings.Session != null && BotSettings.Session.HasEnoughInfo() && WebAccess == null)
                {
                    BotLogger.LogTrace("Trying last saved session.");
                    var webAccess = new SteamWebAccess(
                        BotSettings.Session,
                        IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress) ? ipAddress : IPAddress.Any,
                        string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy)
                    );

                    if (await webAccess.VerifySession().ConfigureAwait(false))
                    {
                        BotLogger.LogTrace("Session is valid.");
                        WebAccess = webAccess;
                        await OnNewWebSessionAvailable(WebAccess.Session).ConfigureAwait(false);

                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                BotLogger.LogWarning(e, "Failed to recover WebSession, error: {0}", e.Message);
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
                        BotLogger.LogWarning(e, e.Message);
                        BotLogger.LogDebug("Sleeping for 60 seconds.");
                        await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, e.Message);
            }

            await OnTerminate().ConfigureAwait(false);
        }
    }
}