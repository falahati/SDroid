using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SDroid.Helpers;
using SDroid.Interfaces;
using SDroid.SteamMobile;
using SDroid.SteamTrade;
using SDroid.SteamTrade.EventArguments;
using SDroid.SteamTrade.Models.Trade;
using SDroid.SteamTrade.Models.TradeOffer;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Exceptions;
using SteamKit2;

namespace SDroid
{
    // ReSharper disable once ClassTooBig
    public abstract class SteamBot : ISteamBot
    {
        protected readonly object LocalLock = new object();
        protected readonly SemaphoreSlim WebSessionLock = new SemaphoreSlim(1, 1);
        private SteamBotStatus _botStatus;
        protected Timer AuthenticatorConfirmationTimer;
        protected CancellationTokenSource CancellationTokenSource;
        protected List<Confirmation> FetchedConfirmations = new List<Confirmation>();
        protected Timer SessionCheckTimer;

        protected SteamBot(IBotSettings settings, ILogger botLogger)
        {
            BotSettings = settings;
            BotLogger = botLogger;
            BotStatus = SteamBotStatus.Ready;
        }

        protected ILogger BotLogger { get; set; }

        protected IBotSettings BotSettings { get; set; }

        public SteamBotStatus BotStatus
        {
            get => _botStatus;
            protected set
            {
                _botStatus = value;
                OnStatusChanged();
            }
        }

        protected virtual SteamID SteamId
        {
            get => WebAccess?.Session?.SteamCommunityId != null ? new SteamID(WebAccess.Session.SteamCommunityId.Value) : null;
        }

        protected SteamWebAccess WebAccess { get; set; }

        protected SteamWebAPI WebAPI { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        ILogger ISteamBot.BotLogger
        {
            get => BotLogger;
        }

        /// <inheritdoc />
        IBotSettings ISteamBot.BotSettings
        {
            get => BotSettings;
        }


        /// <inheritdoc />
        SteamBotStatus ISteamBot.BotStatus
        {
            get => BotStatus;
        }

        /// <inheritdoc />
        SteamID ISteamBot.SteamId
        {
            get => SteamId;
        }

        SteamWebAccess ISteamBot.WebAccess
        {
            get => WebAccess;
        }

        SteamWebAPI ISteamBot.WebAPI
        {
            get => WebAPI;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{GetType()}] {BotSettings.Username} - {BotStatus}";
        }

        public virtual Task StartBot()
        {
            lock (LocalLock)
            {
                if (BotStatus != SteamBotStatus.Ready)
                {
                    return Task.CompletedTask;
                }

                BotStatus = SteamBotStatus.Connected;
            }

            BotLogger.LogInformation("Starting bot...");
            CancellationTokenSource = new CancellationTokenSource();

            return Task.CompletedTask;
        }

        public virtual async Task StopBot()
        {
            lock (LocalLock)
            {
                if (BotStatus == SteamBotStatus.Ready)
                {
                    return;
                }
            }

            BotLogger.LogInformation("Stopping bot.");

            CancellationTokenSource?.Cancel(true);
            SessionCheckTimer?.Dispose();
            AuthenticatorConfirmationTimer?.Dispose();

            BotLogger.LogDebug("Waiting for bot to stop.");

            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            //while (true)
            //{
            //    lock (this.LocalLock)
            //    {
            //        if (BotStatus == SteamBotStatus.Faulted)
            //        {
            //            break;
            //        }
            //    }

            //    await Task.Delay(TimeSpan.FromMilliseconds(300)).ConfigureAwait(false);
            //}

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ITradeOfferBot offerController)
            {
                BotLogger.LogTrace("Disposing ITradeOfferBot's TradeOfferManager.");

                lock (LocalLock)
                {
                    offerController.TradeOfferManager?.Dispose();
                    offerController.TradeOfferManager = null;
                }
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ITradeBot tradeController)
            {
                BotLogger.LogTrace("Disposing ITradeBot's TradeManager.");

                lock (LocalLock)
                {
                    tradeController.TradeManager?.Dispose();
                    tradeController.TradeManager = null;
                }
            }

            BotLogger.LogInformation("Bot stopped.");

            lock (LocalLock)
            {
                BotStatus = SteamBotStatus.Ready;
            }
        }

        // ReSharper disable once FunctionComplexityOverflow
        // ReSharper disable once MethodTooLong
        // ReSharper disable once ExcessiveIndentation
        protected virtual async Task BotLogin()
        {
            lock (LocalLock)
            {
                if (BotStatus == SteamBotStatus.LoggingIn)
                {
                    return;
                }

                BotStatus = SteamBotStatus.LoggingIn;
            }

            BotLogger.LogInformation("Starting login process...");

            try
            {
                // Check if the current session is still valid
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (WebAccess != null)
                {
                    BotLogger.LogTrace("Trying current session.");

                    if (await WebAccess.VerifySession().ConfigureAwait(false))
                    {
                        BotLogger.LogTrace("Session is valid.");
                        await OnNewWebSessionAvailable(WebAccess.Session).ConfigureAwait(false);

                        return;
                    }
                }

                // Check if the bot's authenticator holds a valid session or a session that can be extended
                var webLogin = new WebLogin();

                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is IAuthenticatorBot authenticatorController)
                {
                    webLogin = new MobileLogin();

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

                        BotLogger.LogDebug("Refreshing authenticator session.");

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

                                return;
                            }
                        }
                    }
                }

                // Check to see if we have a valid session saved
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

                        return;
                    }
                }

                // If nothing found, start the login process with the WebLogin or MobileLogin
                await OnLoggingIn().ConfigureAwait(false);

                BotLogger.LogDebug("Requesting account password.");
                var password = await OnPasswordRequired().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(password))
                {
                    BotLogger.LogError("Bad password provided.");
                    await OnTerminate().ConfigureAwait(false);

                    return;
                }

                var loginCredentials = new LoginCredentials(BotSettings.Username, password);
                var backoff = new ExponentialBackoff();

                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    try
                    {
                        await backoff.Delay().ConfigureAwait(false);

                        BotLogger.LogDebug("Logging in using {0}", webLogin.GetType());
                        var session = await webLogin.DoLogin(loginCredentials).ConfigureAwait(false);
                        BotLogger.LogDebug("Logged in using {0}", webLogin.GetType());
                        await OnNewWebSessionAvailable(session).ConfigureAwait(false);

                        return;
                    }
                    catch (UserLoginException e)
                    {
                        BotLogger.LogDebug(e, e.Message);

                        switch (e.ErrorCode)
                        {
                            case UserLoginErrorCode.GeneralFailure:

                                throw;
                            case UserLoginErrorCode.BadRSAResponse:

                                throw;
                            case UserLoginErrorCode.BadCredentials:
                                BotLogger.LogDebug("Requesting account password.");
                                password = await OnPasswordRequired().ConfigureAwait(false);

                                if (string.IsNullOrWhiteSpace(password))
                                {
                                    BotLogger.LogError("Bad password provided.");
                                    await OnTerminate().ConfigureAwait(false);

                                    return;
                                }
                                else
                                {
                                    backoff.Reset();
                                    loginCredentials.Password = password;
                                    loginCredentials.EmailVerificationCode = null;
                                    loginCredentials.CaptchaCode = null;
                                    loginCredentials.TwoFactorAuthenticationCode = null;
                                }

                                break;
                            case UserLoginErrorCode.NeedsCaptchaCode:

                                BotLogger.LogDebug("Downloading captcha image.");
                                var captchaImage = await e.UserLogin.DownloadCaptchaImage().ConfigureAwait(false);
                                BotLogger.LogDebug("Requesting captcha code.");
                                var captchaCode = await OnCaptchaCodeRequired(captchaImage).ConfigureAwait(false);

                                if (string.IsNullOrWhiteSpace(captchaCode))
                                {
                                    BotLogger.LogError("Bad captcha code provided.");
                                    await OnTerminate().ConfigureAwait(false);

                                    return;
                                }
                                else
                                {
                                    backoff.Reset();
                                    loginCredentials.CaptchaCode = captchaCode;
                                }

                                break;
                            case UserLoginErrorCode.NeedsTwoFactorAuthenticationCode:
                                BotLogger.LogDebug("Requesting authenticator code.");
                                var mobileAuthCode = await OnAuthenticatorCodeRequired().ConfigureAwait(false);

                                if (string.IsNullOrWhiteSpace(mobileAuthCode))
                                {
                                    BotLogger.LogError("Bad authenticator code provided.");
                                    await OnTerminate().ConfigureAwait(false);

                                    return;
                                }
                                else
                                {
                                    backoff.Reset();
                                    loginCredentials.TwoFactorAuthenticationCode = mobileAuthCode;
                                }

                                break;
                            case UserLoginErrorCode.NeedsEmailVerificationCode:
                                BotLogger.LogDebug("Requesting email verification code.");
                                var emailAuthCode = await OnEmailCodeRequired().ConfigureAwait(false);

                                if (string.IsNullOrWhiteSpace(emailAuthCode))
                                {
                                    BotLogger.LogError("Bad email verification code provided.");
                                    await OnTerminate().ConfigureAwait(false);

                                    return;
                                }
                                else
                                {
                                    backoff.Reset();
                                    loginCredentials.EmailVerificationCode = emailAuthCode;
                                }

                                break;
                            case UserLoginErrorCode.TooManyFailedLoginAttempts:

                                // ignore
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, e.Message);
                await OnTerminate().ConfigureAwait(false);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            StopBot().Wait();
            SessionCheckTimer?.Dispose();
            AuthenticatorConfirmationTimer?.Dispose();
            CancellationTokenSource?.Dispose();
            WebSessionLock?.Dispose();
            WebAccess = null;
            WebAPI = null;
        }

        protected virtual async Task OnAuthenticatorCheckConfirmations()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is IAuthenticatorBot authenticatorBot)
            {
                try
                {
                    lock (LocalLock)
                    {
                        if (
                            BotStatus != SteamBotStatus.Running ||
                            authenticatorBot.BotAuthenticatorSettings.Authenticator == null
                        )
                        {
                            return;
                        }
                    }

                    BotLogger.LogDebug("Retrieving the list of authenticator pending confirmations.");
                    var confirmations = await (
                        authenticatorBot.BotAuthenticatorSettings?.Authenticator?.FetchConfirmations()
                    ).ConfigureAwait(false);

                    foreach (var confirmation in confirmations ?? new Confirmation[0])
                    {
                        var isNew = false;

                        lock (FetchedConfirmations)
                        {
                            if (!FetchedConfirmations.Contains(confirmation))
                            {
                                isNew = true;
                                FetchedConfirmations.Add(confirmation);
                            }
                        }

                        if (isNew)
                        {
                            await authenticatorBot.OnAuthenticatorConfirmationAvailable(confirmation).ConfigureAwait(false);
                        }
                    }

                    lock (FetchedConfirmations)
                    {
                        foreach (var confirmation in FetchedConfirmations.ToArray())
                        {
                            if (confirmations?.Contains(confirmation) == false)
                            {
                                FetchedConfirmations.Remove(confirmation);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    BotLogger.LogWarning(e, e.Message);
                }
            }
        }

        protected virtual async Task<string> OnAuthenticatorCodeRequired()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(this is IAuthenticatorBot authenticator))
            {
                throw new NotImplementedException();
            }

            while (authenticator.BotAuthenticatorSettings.Authenticator == null)
            {
                CancellationTokenSource.Token.ThrowIfCancellationRequested();

                BotLogger.LogDebug("Waiting for authenticator to become available.");

                await Task.WhenAll(
                    authenticator.OnAuthenticatorMissing(),
                    Task.Delay(500)
                ).ConfigureAwait(false);
            }

            BotLogger.LogDebug("Generating Steam guard code via authenticator.");

            return await authenticator.BotAuthenticatorSettings.Authenticator.GenerateSteamGuardCode().ConfigureAwait(false);
        }

        protected virtual Task<string> OnCaptchaCodeRequired(byte[] captchaImageBinary)
        {
            throw new NotImplementedException();
        }

        protected virtual async Task OnCheckSession()
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

                try
                {
                    if (await WebAccess.VerifySession().ConfigureAwait(false))
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    BotLogger.LogWarning(e, e.Message);
                }

                BotLogger.LogDebug("Session expired. Forcefully starting a new login process.");
                await BotLogin().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, e.Message);
                await OnLoggedOut().ConfigureAwait(false);
                await OnTerminate().ConfigureAwait(false);
            }
        }

        protected virtual Task<string> OnEmailCodeRequired()
        {
            throw new NotImplementedException();
        }

        protected virtual Task OnLoggedIn()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnLoggedOut()
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnLoggingIn()
        {
            return Task.CompletedTask;
        }

        // ReSharper disable once FunctionComplexityOverflow
        protected virtual async Task OnNewWebSessionAvailable(WebSession session)
        {
            try
            {
                await WebSessionLock.WaitAsync().ConfigureAwait(false);

                BotLogger.LogTrace(
                    "Session changing. WebSession.HasEnoughInfo() = `{0}`",
                    session?.HasEnoughInfo().ToString() ?? "NULL"
                );
                session = session ?? new WebSession();

                // Save the latest session to bot's settings
                BotSettings.Session = session;
                BotSettings.SaveSettings();

                // Update authenticator session
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is IAuthenticatorBot authenticatorController)
                {
                    BotLogger.LogTrace("Updating IAuthenticatorBot's session.");

                    if (
                        session is MobileSession mobileSession &&
                        authenticatorController.BotAuthenticatorSettings?.Authenticator != null
                    )
                    {
                        authenticatorController.BotAuthenticatorSettings.Authenticator = new Authenticator(
                            authenticatorController.BotAuthenticatorSettings.Authenticator.AuthenticatorData,
                            mobileSession, authenticatorController.BotAuthenticatorSettings.Authenticator.DeviceId
                        );
                    }
                    else
                    {
                        authenticatorController.BotAuthenticatorSettings?.Authenticator?.Session?.UpdateSession(session);
                    }

                    authenticatorController.BotAuthenticatorSettings?.SaveSettings();
                }

                // Create a SteamWebAccess if missing or update the current SteamWebAccess
                if (WebAccess == null)
                {
                    BotLogger.LogTrace("Initializing an instance of WebAccess.");
                    WebAccess = new SteamWebAccess(
                        session,
                        IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress) ? ipAddress : IPAddress.Any,
                        string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy)
                    );
                }
                else
                {
                    BotLogger.LogTrace("Updating WebAccess's session.");
                    WebAccess.Session = session;
                }

                // If APIKey is missing, request one
                if (string.IsNullOrWhiteSpace(BotSettings.ApiKey))
                {
                    string apiKey = null;

                    while (string.IsNullOrWhiteSpace(apiKey))
                    {
                        CancellationTokenSource.Token.ThrowIfCancellationRequested();
                        var backoff = new ExponentialBackoff();

                        try
                        {
                            BotLogger.LogDebug("API key is missing, retriving directly from Steam.");
                            await backoff.Delay().ConfigureAwait(false);
                            apiKey = await SteamWebAPI.GetApiKey(WebAccess).ConfigureAwait(false);

                            if (string.IsNullOrWhiteSpace(apiKey))
                            {
                                BotLogger.LogDebug("Account not registered for an API key.");

                                var domainName = BotSettings.DomainName;

                                if (string.IsNullOrWhiteSpace(domainName))
                                {
                                    domainName = (await (WebAccess ?? SteamWebAccess.GetGuest())
                                                     .GetActualIPAddress().ConfigureAwait(false))?.ToString() ??
                                                 "example.com";
                                }

                                BotLogger.LogDebug("Registering for a new API key. DomainName = `{0}`", domainName);

                                if (await SteamWebAPI
                                    .RegisterApiKey(WebAccess, domainName)
                                    .ConfigureAwait(false))
                                {
                                    BotLogger.LogDebug("API key registered.");
                                }
                                else
                                {
                                    BotLogger.LogWarning("Failed to register API key. Another try in 30 seconds.");
                                    await Task.Delay(
                                        TimeSpan.FromSeconds(30),
                                        CancellationTokenSource.Token
                                    ).ConfigureAwait(false);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            BotLogger.LogWarning(e, e.Message);
                            // ignored
                        }
                    }

                    // If API key acquired, save to bot's settings
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        BotLogger.LogDebug("Saving API key to bot's settings.");
                        BotSettings.ApiKey = apiKey;
                        BotSettings.SaveSettings();
                    }
                }

                // If API key is available, create or update the SteamWebAPI
                if (!string.IsNullOrWhiteSpace(BotSettings.ApiKey))
                {
                    if (WebAPI == null)
                    {
                        BotLogger.LogTrace("Initializing an instance of WebAPI.");
                        WebAPI = new SteamWebAPI(BotSettings.ApiKey, WebAccess);
                    }
                    else
                    {
                        BotLogger.LogTrace("Updating WebAPI's APIKey.");
                        WebAPI.ApiKey = BotSettings.ApiKey;
                    }
                }

                // Create a TradeOfferManager for classes implementing ITradeOfferController
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is ITradeOfferBot offerController)
                {
                    if (offerController.TradeOfferManager == null)
                    {
                        BotLogger.LogTrace("Initializing an instance of TradeOfferManager for ITradeOfferBot.");
                        offerController.TradeOfferManager = new TradeOfferManager(
                            WebAPI,
                            WebAccess,
                            offerController.TradeOfferBotSettings.TradeOfferOptions ?? TradeOfferOptions.Default
                        );
                        offerController.TradeOfferManager.TradeOfferAccepted += OnInternalTradeOfferAccepted;
                        offerController.TradeOfferManager.TradeOfferCanceled += OnInternalTradeOfferCanceled;
                        offerController.TradeOfferManager.TradeOfferChanged += OnInternalTradeOfferChanged;
                        offerController.TradeOfferManager.TradeOfferDeclined += OnInternalOfferDeclined;
                        offerController.TradeOfferManager.TradeOfferInEscrow += OnInternalOfferInEscrow;
                        offerController.TradeOfferManager.TradeOfferNeedsConfirmation += OnInternalTradeOfferNeedsConfirmation;
                        offerController.TradeOfferManager.TradeOfferReceived += OnInternalTradeOfferReceived;
                        offerController.TradeOfferManager.TradeOfferSent += OnInternalTradeOfferSent;
                        offerController.TradeOfferManager.StartPolling();
                    }
                }

                // Create a TradeManager for classes implementing ITradeController
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is ITradeBot tradeController)
                {
                    if (tradeController.TradeManager == null)
                    {
                        BotLogger.LogTrace("Initializing an instance of TradeManager for ITradeBot.");
                        tradeController.TradeManager = new TradeManager(
                            WebAPI,
                            WebAccess,
                            tradeController.TradeBotSettings.TradeOptions ?? TradeOptions.Default
                        );
                        tradeController.TradeManager.TradeCreated += OnInternalTradeCreated;
                    }
                }

                BotLogger.LogDebug("Session updated successfully.");

                lock (LocalLock)
                {
                    // If bot already logged it and this is a session update, just make sure bot is running
                    if (BotStatus != SteamBotStatus.LoggingIn)
                    {
                        BotStatus = SteamBotStatus.Running;

                        return;
                    }
                }

                BotLogger.LogTrace("Initializing Session check timer.");

                // If this is an actual login, start the session check timer and change the bot's status
                SessionCheckTimer = new Timer(
                    async state =>
                    {
                        await OnCheckSession().ConfigureAwait(false);

                        if (CancellationTokenSource?.IsCancellationRequested == false)
                        {
                            SessionCheckTimer?.Change(
                                TimeSpan.FromSeconds(BotSettings.SessionCheckInterval),
                                TimeSpan.FromMilliseconds(-1)
                            );
                        }
                        else
                        {
                            await OnTerminate().ConfigureAwait(false);
                        }
                    },
                    null,
                    TimeSpan.FromSeconds(BotSettings.SessionCheckInterval),
                    TimeSpan.FromMilliseconds(-1)
                );

                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is IAuthenticatorBot authenticatorBot)
                {
                    BotLogger.LogTrace("Initializing IAuthenticatorBot's Authenticator Confirmation Timer.");
                    AuthenticatorConfirmationTimer = new Timer(
                        async state =>
                        {
                            await OnAuthenticatorCheckConfirmations().ConfigureAwait(false);

                            if (CancellationTokenSource?.IsCancellationRequested == false)
                            {
                                AuthenticatorConfirmationTimer?.Change(
                                    TimeSpan.FromSeconds(
                                        authenticatorBot.BotAuthenticatorSettings.ConfirmationCheckInterval
                                    ),
                                    TimeSpan.FromMilliseconds(-1)
                                );
                            }
                        },
                        null,
                        TimeSpan.FromSeconds(authenticatorBot.BotAuthenticatorSettings.ConfirmationCheckInterval),
                        TimeSpan.FromMilliseconds(-1)
                    );
                }

                lock (LocalLock)
                {
                    BotStatus = SteamBotStatus.Running;
                }

                BotLogger.LogDebug("Logged in successfully.");
                await OnLoggedIn().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // On failure, terminate the bot
                BotLogger.LogError(e, e.Message);
                await OnTerminate().ConfigureAwait(false);
            }
            finally
            {
                WebSessionLock.Release();
            }
        }

        protected virtual Task<string> OnPasswordRequired()
        {
            throw new NotImplementedException();
        }

        protected virtual void OnStatusChanged()
        {
            // ignore
        }

        protected virtual async Task OnTerminate()
        {
            BotLogger.LogCritical("Terminating due to a fault. See previous logs.");

            lock (LocalLock)
            {
                BotStatus = SteamBotStatus.Faulted;
            }

            await StopBot().ConfigureAwait(false);
        }

        private async void OnInternalOfferDeclined(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs
        )
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(this is ITradeOfferBot tradeOfferBot))
            {
                return;
            }

            BotLogger.LogTrace(
                "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId
            );

            try
            {
                await tradeOfferBot.OnTradeOfferDeclined(
                    tradeOfferStateChangedEventArgs.TradeOffer
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                tradeOfferStateChangedEventArgs.Processed = false;
                BotLogger.LogWarning(e, "Event failed with message: {0}", e.Message);
            }
        }

        private async void OnInternalOfferInEscrow(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs
        )
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(this is ITradeOfferBot tradeOfferBot))
            {
                return;
            }

            BotLogger.LogTrace(
                "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId
            );

            try
            {
                await tradeOfferBot.OnTradeOfferInEscrow(
                    tradeOfferStateChangedEventArgs.TradeOffer
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                tradeOfferStateChangedEventArgs.Processed = false;
                BotLogger.LogWarning(e, "Event failed with message: {0}", e.Message);
            }
        }

        private async void OnInternalTradeCreated(object sender, TradeCreatedEventArgs tradeCreatedEventArgs)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(this is ITradeBot tradeBot))
            {
                return;
            }

            BotLogger.LogTrace(
                "TradeCreatedEventArgs.PartnerSteamId = `{0}`",
                tradeCreatedEventArgs.PartnerSteamId
            );

            try
            {
                await tradeBot.OnTradeCreated(
                    tradeCreatedEventArgs.PartnerSteamId,
                    tradeCreatedEventArgs.Trade
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                BotLogger.LogWarning(e, "Event failed with message: {0}", e.Message);
            }
        }

        private async void OnInternalTradeOfferAccepted(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs
        )
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(this is ITradeOfferBot tradeOfferBot))
            {
                return;
            }

            BotLogger.LogTrace(
                "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId
            );

            try
            {
                await tradeOfferBot.OnTradeOfferAccepted(
                    tradeOfferStateChangedEventArgs.TradeOffer
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                tradeOfferStateChangedEventArgs.Processed = false;
                BotLogger.LogWarning(e, "Event failed with message: {0}", e.Message);
            }
        }

        private async void OnInternalTradeOfferCanceled(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs
        )
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(this is ITradeOfferBot tradeOfferBot))
            {
                return;
            }

            BotLogger.LogTrace(
                "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId
            );

            try
            {
                await tradeOfferBot.OnTradeOfferCanceled(
                    tradeOfferStateChangedEventArgs.TradeOffer
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                tradeOfferStateChangedEventArgs.Processed = false;
                BotLogger.LogWarning(e, "Event failed with message: {0}", e.Message);
            }
        }

        private async void OnInternalTradeOfferChanged(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs
        )
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(this is ITradeOfferBot tradeOfferBot))
            {
                return;
            }

            BotLogger.LogTrace(
                "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId
            );

            try
            {
                await tradeOfferBot.OnTradeOfferChanged(
                    tradeOfferStateChangedEventArgs.TradeOffer
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                tradeOfferStateChangedEventArgs.Processed = false;
                BotLogger.LogWarning(e, "Event failed with message: {0}", e.Message);
            }
        }

        private async void OnInternalTradeOfferNeedsConfirmation(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs
        )
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(this is ITradeOfferBot tradeOfferBot))
            {
                return;
            }

            BotLogger.LogTrace(
                "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId
            );

            try
            {
                await tradeOfferBot.OnTradeOfferNeedsConfirmation(
                    tradeOfferStateChangedEventArgs.TradeOffer
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                tradeOfferStateChangedEventArgs.Processed = false;
                BotLogger.LogWarning(e, "Event failed with message: {0}", e.Message);
            }
        }

        private async void OnInternalTradeOfferReceived(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs
        )
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(this is ITradeOfferBot tradeOfferBot))
            {
                return;
            }

            BotLogger.LogTrace(
                "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId
            );

            try
            {
                await tradeOfferBot.OnTradeOfferReceived(
                    tradeOfferStateChangedEventArgs.TradeOffer
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                tradeOfferStateChangedEventArgs.Processed = false;
                BotLogger.LogWarning(e, "Event failed with message: {0}", e.Message);
            }
        }

        private async void OnInternalTradeOfferSent(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs
        )
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (!(this is ITradeOfferBot tradeOfferBot))
            {
                return;
            }

            BotLogger.LogTrace(
                "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId
            );

            try
            {
                await tradeOfferBot.OnTradeOfferSent(
                    tradeOfferStateChangedEventArgs.TradeOffer
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                tradeOfferStateChangedEventArgs.Processed = false;
                BotLogger.LogWarning(e, "Event failed with message: {0}", e.Message);
            }
        }
    }
}