﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SDroid.Helpers;
using SDroid.Interfaces;
using SDroid.SteamKit;
using SDroid.SteamMobile;
using SDroid.SteamMobile.Exceptions;
using SDroid.SteamWeb;
using SteamKit2;
using static SteamKit2.Internal.CMsgRemoteClientBroadcastStatus;
using SteamKit2.Authentication;
using static SteamKit2.SteamUser;
using SteamKit2.Internal;

namespace SDroid
{
    // ReSharper disable once ClassTooBig
    public abstract class SteamKitBot : SteamBot, ISteamKitBot, SteamKit2.Authentication.IAuthenticator
    {
        protected ExponentialBackoff ConnectionBackoff = new ExponentialBackoff(20);
        protected ExponentialBackoff LoginBackoff = new ExponentialBackoff(10);
        protected AuthSessionDetails LoginDetails;
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
            get => SteamClient?.SteamID ?? new SteamID(BotSettings.Session.SteamId);
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
            LoginBackoff.Reset();
            ConnectionBackoff.Reset();
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

        //protected override async Task OnCheckSession()
        //{
        //    try
        //    {
        //        lock (LocalLock)
        //        {
        //            if (BotStatus != SteamBotStatus.Running || WebAccess == null)
        //            {
        //                return;
        //            }
        //        }

        //        BotLogger.LogDebug("[{0}] Checking session...", SteamId?.ConvertToUInt64());

        //        if (await WebAccess.VerifySession().ConfigureAwait(false))
        //        {
        //            return;
        //        }

        //        BotLogger.LogDebug("[{0}] Session expired.", SteamId?.ConvertToUInt64());
        //    }
        //    catch (Exception e)
        //    {
        //        BotLogger.LogError(e, "[{0}] {1}", SteamId?.ConvertToUInt64(), e.Message);
        //        lock (LocalLock)
        //        {
        //            StalledLoginCheckTimer?.Dispose();
        //        }
        //        await OnLoggedOut().ConfigureAwait(false);
        //        await OnTerminate(false).ConfigureAwait(false);
        //    }
        //}

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

                LoginDetails = LoginDetails ?? new AuthSessionDetails
                {
                    Username = BotSettings.Username,
                    IsPersistentSession = true,
                    GuardData = BotSettings.GuardData,
                    Authenticator = this,
                    ClientOSType = EOSType.Android9,
                    PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp,
                };

                if (string.IsNullOrWhiteSpace(LoginDetails.Password))
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
                        await OnTerminate(false).ConfigureAwait(false);

                        return;
                    }

                    LoginDetails.Password = password;
                }
                
                var authSession = await SteamClient.Authentication.BeginAuthSessionViaCredentialsAsync(LoginDetails);
                var pollResponse = await authSession.PollingWaitForResultAsync();

                if (pollResponse.NewGuardData != null)
                {
                    BotSettings.GuardData = pollResponse.NewGuardData;
                    BotSettings.SaveSettings();
                }

                BotLogger.LogTrace("[{0}] Retrieving WebAPI and WebAccess session.", SteamId?.ConvertToUInt64());
                
                var success = false;
                var session = new MobileSession(
                    authSession.SteamID,
                    $"{authSession.SteamID}%7C%7C{pollResponse.AccessToken}",
                    SteamMobileWebAccess.GetGuest().Session.SessionId,
                    pollResponse.AccessToken,
                    pollResponse.RefreshToken
                );
                
                //if (
                //    new SteamWebAccess(
                //        session,
                //        IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress)
                //            ? ipAddress
                //            : IPAddress.Any,
                //        string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy)
                //    ).VerifySession().Result
                //)
                //{
                //    BotLogger.LogTrace("[{0}] Session is valid.", SteamId?.ConvertToUInt64());
                    OnNewWebSessionAvailable(session).Wait();
                    LoginBackoff.Reset();
                    success = true;
                // }

                //if (!success)
                //{
                //    BotLogger.LogDebug("[{0}] Failed to retrieve WebAccess session. Trying to recover session.", SteamId?.ConvertToUInt64());

                //    if (RecoverWebSession().Result)
                //    {
                //        LoginBackoff.Reset();
                //    }
                //    else
                //    {
                //        BotLogger.LogDebug("[{0}] Failed to get a WebAccess session.", SteamId?.ConvertToUInt64());
                //        await OnTerminate(false);
                //        return;
                //    }
                //}

                SteamUser.LogOn(
                    new LogOnDetails
                    {
                        Username = pollResponse.AccountName,
                        AccessToken = pollResponse.RefreshToken,
                        ShouldRememberPassword = true,
                        ClientOSType = EOSType.Android9,
                        LoginID = (uint)(new Random().Next(10000, 10000000)),
                    }
                );
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
                EResult.AccountLoginDeniedThrottle == loggedOffCallback.Result ||
                EResult.Fail == loggedOffCallback.Result
            )
            {
                // InternalInitializeLogin().Wait();
            }
            else
            {
                OnLoggedOut();
            }
        }

        // ReSharper disable once MethodTooLong
        // ReSharper disable once ExcessiveIndentation
        private async void OnInternalSteamUserLoggedOn(SteamUser.LoggedOnCallback loggedOnCallback)
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

                return;
            }
            
            if (loggedOnCallback.Result == EResult.InvalidPassword)
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
                    OnTerminate(false).Wait();

                    return;
                }

                if (LoginBackoff.Attempts >= 3)
                {
                    OnTerminate(false).Wait();
                    return;
                }

                LoginDetails.Password = password;
            }
            else if (loggedOnCallback.Result == EResult.ServiceUnavailable || loggedOnCallback.Result == EResult.TryAnotherCM)
            {
                lock (LocalLock)
                {
                    StalledLoginCheckTimer?.Dispose();
                }

                OnTerminate(false).Wait();
                return;
            }
            else if (loggedOnCallback.Result == EResult.RateLimitExceeded)
            {
                // ignore
            }
            else
            {
                OnTerminate(true).Wait();
                return;
            }

            InternalInitializeLogin().Wait();
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

            await OnTerminate(false).ConfigureAwait(false);
        }

        async Task<string> IAuthenticator.GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        {
            await LoginBackoff.Delay().ConfigureAwait(false);

            BotLogger.LogTrace("[{0}] Realigning authenticator clock.", SteamId?.ConvertToUInt64());
            await SteamTime.ReAlignTime();

            BotLogger.LogDebug("[{0}] Requesting authenticator code.", SteamId?.ConvertToUInt64());
            var mobileAuthCode = await OnAuthenticatorCodeRequired();

            if (string.IsNullOrEmpty(mobileAuthCode))
            {
                BotLogger.LogError("[{0}] Bad authenticator code provided.", SteamId?.ConvertToUInt64());
                lock (LocalLock)
                {
                    StalledLoginCheckTimer?.Dispose();
                }

                await OnTerminate(false);

                return null;
            }

            if (previousCodeWasIncorrect)
            {
                if (LoginBackoff.Attempts >= 3)
                {
                    await OnTerminate(false);
                    return null;
                }
            }
            else
            {
                LoginBackoff.Reset(1);
            }

            return mobileAuthCode;
        }

        public async Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        {
            await LoginBackoff.Delay().ConfigureAwait(false);

            BotLogger.LogDebug("[{0}] Requesting email verification code for {1}", SteamId?.ConvertToUInt64(), email);
            var emailAuthCode = await OnEmailCodeRequired();

            if (string.IsNullOrEmpty(emailAuthCode))
            {
                BotLogger.LogError("[{0}] Bad email verification code provided.", SteamId?.ConvertToUInt64());
                lock (LocalLock)
                {
                    StalledLoginCheckTimer?.Dispose();
                }

                await OnTerminate(false);

                return null;
            }

            if (previousCodeWasIncorrect)
            {
                if (LoginBackoff.Attempts >= 3)
                {
                    await OnTerminate(false);
                    return null;
                }
            }
            else
            {
                LoginBackoff.Reset(1);
            }

            return emailAuthCode;
        }

        public async Task<bool> AcceptDeviceConfirmationAsync()
        {
            await LoginBackoff.Delay().ConfigureAwait(false);

            BotLogger.LogDebug("[{0}] Requesting device confirmation.", SteamId?.ConvertToUInt64());
            var result = await OnDeviceConfirmationRequired();

            if (!result)
            {
                if (LoginBackoff.Attempts >= 3)
                {
                    await OnTerminate(false);
                    return false;
                }
            }
            else
            {
                LoginBackoff.Reset(1);
            }

            return result;
        }
    }
}