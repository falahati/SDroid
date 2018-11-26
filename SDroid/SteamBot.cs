using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
        protected readonly SemaphoreSlim WebSessionLock = new SemaphoreSlim(1, 1);
        protected Timer AuthenticatorConfirmationTimer;
        protected CancellationTokenSource CancellationTokenSource;
        protected List<Confirmation> FetchedConfirmations = new List<Confirmation>();
        protected Timer SessionCheckTimer;

        protected SteamBot(IBotSettings settings, IBotLogger botLogger)
        {
            BotSettings = settings;
            BotLogger = botLogger;
            BotStatus = SteamBotStatus.Ready;
        }

        public virtual void Dispose()
        {
            StopBot().Wait();
            SessionCheckTimer?.Dispose();
            AuthenticatorConfirmationTimer?.Dispose();
            CancellationTokenSource?.Dispose();
            WebSessionLock?.Dispose();
            WebAccess = null;
            WebAPI = null;
        }

        public IBotLogger BotLogger { get; protected set; }
        public IBotSettings BotSettings { get; protected set; }
        public SteamBotStatus BotStatus { get; protected set; }

        /// <inheritdoc />
        public virtual SteamID SteamId
        {
            get
            {
                if (WebAccess?.Session?.SteamCommunityId != null)
                {
                    return new SteamID(WebAccess.Session.SteamCommunityId.Value);
                }

                return null;
            }
        }

        public SteamWebAccess WebAccess { get; protected set; }
        public SteamWebAPI WebAPI { get; protected set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{GetType()}] {BotSettings.Username} - {BotStatus}";
        }

        public virtual async Task StartBot()
        {
            lock (this)
            {
                if (BotStatus != SteamBotStatus.Ready)
                {
                    return;
                }

                BotStatus = SteamBotStatus.Connected;
            }

            await BotLogger.Debug(nameof(StartBot), "Starting bot.").ConfigureAwait(false);

            CancellationTokenSource = new CancellationTokenSource();
        }

        public virtual async Task StopBot()
        {
            await BotLogger.Debug(nameof(StopBot), "Stopping bot.").ConfigureAwait(false);
            CancellationTokenSource?.Cancel(true);
            SessionCheckTimer?.Dispose();
            AuthenticatorConfirmationTimer?.Dispose();

            await BotLogger.Debug(nameof(StopBot), "Waiting for bot to stop.").ConfigureAwait(false);
            
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            //while (true)
            //{
            //    lock (this)
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
                await BotLogger.Debug(nameof(StopBot), "Disposing ITradeOfferBot's TradeOfferManager.")
                    .ConfigureAwait(false);

                lock (this)
                {
                    offerController.TradeOfferManager?.Dispose();
                    offerController.TradeOfferManager = null;
                }
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ITradeBot tradeController)
            {
                await BotLogger.Debug(nameof(StopBot), "Disposing ITradeBot's TradeManager.").ConfigureAwait(false);

                lock (this)
                {
                    tradeController.TradeManager?.Dispose();
                    tradeController.TradeManager = null;
                }
            }

            await BotLogger.Debug(nameof(StopBot), "Bot stopped.").ConfigureAwait(false);

            lock (this)
            {
                BotStatus = SteamBotStatus.Ready;
            }
        }

        // ReSharper disable once FunctionComplexityOverflow
        // ReSharper disable once MethodTooLong
        protected virtual async Task BotLogin()
        {
            lock (this)
            {
                if (BotStatus == SteamBotStatus.LoggingIn)
                {
                    return;
                }

                BotStatus = SteamBotStatus.LoggingIn;
            }

            await BotLogger.Debug(nameof(BotLogin), "Starting login process.").ConfigureAwait(false);

            try
            {
                // Check if the current session is still valid
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (WebAccess != null)
                {
                    await BotLogger.Debug(nameof(BotLogin), "Trying current session.").ConfigureAwait(false);

                    if (await WebAccess.VerifySession().ConfigureAwait(false))
                    {
                        await BotLogger.Debug(nameof(BotLogin), "Session is valid.").ConfigureAwait(false);
                        await OnNewWebSessionAvailable(WebAccess.Session).ConfigureAwait(false);

                        return;
                    }
                }

                // Check to see if we have a valid session saved
                if (BotSettings.Session != null && BotSettings.Session.HasEnoughInfo() && WebAccess == null)
                {
                    await BotLogger.Debug(nameof(BotLogin), "Trying last saved session.").ConfigureAwait(false);
                    var webAccess = new SteamWebAccess(
                        BotSettings.Session,
                        IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress) ? ipAddress : IPAddress.Any,
                        string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy));

                    if (await webAccess.VerifySession().ConfigureAwait(false))
                    {
                        await BotLogger.Debug(nameof(BotLogin), "Session is valid.").ConfigureAwait(false);
                        WebAccess = webAccess;
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

                    await BotLogger.Debug(nameof(BotLogin), "Trying authenticator session.").ConfigureAwait(false);

                    if (authenticatorController.BotAuthenticatorSettings?.Authenticator?.Session != null)
                    {
                        await BotLogger.Debug(nameof(BotLogin), "Refreshing authenticator session.")
                            .ConfigureAwait(false);

                        if (await
                            authenticatorController.BotAuthenticatorSettings.Authenticator.Session.RefreshSession()
                                .ConfigureAwait(false))
                        {
                            await BotLogger.Debug(nameof(BotLogin), "Authenticator session is valid.")
                                .ConfigureAwait(false);
                            await OnNewWebSessionAvailable(
                                authenticatorController.BotAuthenticatorSettings.Authenticator.Session
                            ).ConfigureAwait(false);

                            return;
                        }
                    }
                }

                // If nothing found, start the login process with the WebLogin or MobileLogin
                await OnLoggingIn().ConfigureAwait(false);

                await BotLogger.Debug(nameof(BotLogin), "Requesting account password.").ConfigureAwait(false);
                var password = await OnPasswordRequired().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(password))
                {
                    await BotLogger.Error(nameof(BotLogin), "Bad password provided.").ConfigureAwait(false);
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
                        await BotLogger.Debug(nameof(BotLogin), "Logging in using " + webLogin.GetType())
                            .ConfigureAwait(false);
                        var session = await webLogin.DoLogin(loginCredentials).ConfigureAwait(false);

                        await BotLogger.Debug(nameof(BotLogin), "Logged in using " + webLogin.GetType())
                            .ConfigureAwait(false);
                        await OnNewWebSessionAvailable(session).ConfigureAwait(false);

                        return;
                    }
                    catch (UserLoginException e)
                    {
                        await BotLogger.Debug(nameof(BotLogin), e.Message).ConfigureAwait(false);

                        switch (e.ErrorCode)
                        {
                            case UserLoginErrorCode.GeneralFailure:

                                throw;
                            case UserLoginErrorCode.BadRSAResponse:

                                throw;
                            case UserLoginErrorCode.BadCredentials:
                                await BotLogger.Debug(nameof(BotLogin), "Requesting account password.")
                                    .ConfigureAwait(false);
                                password = await OnPasswordRequired().ConfigureAwait(false);

                                if (string.IsNullOrWhiteSpace(password))
                                {
                                    await BotLogger.Error(nameof(BotLogin), "Bad password provided.")
                                        .ConfigureAwait(false);
                                    await OnTerminate().ConfigureAwait(false);

                                    return;
                                }
                                else
                                {
                                    backoff.Reset();
                                    loginCredentials.Password = password;
                                }

                                break;
                            case UserLoginErrorCode.NeedsCaptchaCode:

                                await BotLogger.Debug(nameof(BotLogin), "Downloading captcha image.")
                                    .ConfigureAwait(false);
                                var captchaImage = await e.UserLogin.DownloadCaptchaImage().ConfigureAwait(false);
                                await BotLogger.Debug(nameof(BotLogin), "Requesting captcha code.")
                                    .ConfigureAwait(false);
                                var captchaCode = await OnCaptchaCodeRequired(captchaImage).ConfigureAwait(false);

                                if (string.IsNullOrWhiteSpace(captchaCode))
                                {
                                    await BotLogger.Error(nameof(BotLogin), "Bad captcha code provided.")
                                        .ConfigureAwait(false);
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
                                await BotLogger.Debug(nameof(BotLogin), "Requesting authenticator code.")
                                    .ConfigureAwait(false);
                                var mobileAuthCode = await OnAuthenticatorCodeRequired().ConfigureAwait(false);

                                if (string.IsNullOrWhiteSpace(mobileAuthCode))
                                {
                                    await BotLogger.Error(nameof(BotLogin), "Bad authenticator code provided.")
                                        .ConfigureAwait(false);
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
                                await BotLogger.Debug(nameof(BotLogin), "Requesting email verification code.")
                                    .ConfigureAwait(false);
                                var emailAuthCode = await OnEmailCodeRequired().ConfigureAwait(false);

                                if (string.IsNullOrWhiteSpace(emailAuthCode))
                                {
                                    await BotLogger.Error(nameof(BotLogin), "Bad email verification code provided.")
                                        .ConfigureAwait(false);
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
                await BotLogger.Error(nameof(BotLogin), e.Message).ConfigureAwait(false);
                await OnTerminate().ConfigureAwait(false);
            }
        }

        protected virtual async Task OnAuthenticatorCheckConfirmations()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is IAuthenticatorBot authenticatorBot)
            {
                try
                {
                    lock (this)
                    {
                        if (BotStatus != SteamBotStatus.Running ||
                            authenticatorBot.BotAuthenticatorSettings.Authenticator == null)
                        {
                            return;
                        }
                    }

                    await BotLogger.Debug(nameof(OnAuthenticatorCheckConfirmations),
                        "Retrieving the list of authenticator pending confirmations.").ConfigureAwait(false);
                    var confirmations =
                        await (authenticatorBot.BotAuthenticatorSettings?.Authenticator?.FetchConfirmations())
                            .ConfigureAwait(false);

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
                            await authenticatorBot.OnAuthenticatorConfirmationAvailable(confirmation)
                                .ConfigureAwait(false);
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
                    await BotLogger.Warning(nameof(OnAuthenticatorCheckConfirmations), e.Message).ConfigureAwait(false);
                }
            }
        }

        protected virtual async Task<string> OnAuthenticatorCodeRequired()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is IAuthenticatorBot authenticator)
            {
                while (authenticator.BotAuthenticatorSettings.Authenticator == null)
                {
                    await BotLogger
                        .Debug(nameof(OnAuthenticatorCodeRequired), "Waiting for authenticator to become available.")
                        .ConfigureAwait(false);
                    await authenticator.OnAuthenticatorMissing().ConfigureAwait(false);
                }

                await BotLogger
                    .Debug(nameof(OnAuthenticatorCodeRequired), "Generating Steam guard code via authenticator.")
                    .ConfigureAwait(false);

                return await authenticator.BotAuthenticatorSettings.Authenticator.GenerateSteamGuardCode()
                    .ConfigureAwait(false);
            }

            throw new NotImplementedException();
        }

        protected virtual Task<string> OnCaptchaCodeRequired(byte[] captchaImageBinary)
        {
            throw new NotImplementedException();
        }

        protected virtual async Task OnCheckSession()
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
                        .Warning(nameof(OnCheckSession), "Session expired. Forcefully starting a new login process.")
                        .ConfigureAwait(false);

                    await BotLogin().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                await BotLogger.Error(nameof(OnCheckSession), e.Message).ConfigureAwait(false);
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

                await BotLogger.Debug(nameof(OnNewWebSessionAvailable),
                        "Session changing. WebSession.HasEnoughInfo() = `{0}`",
                        session?.HasEnoughInfo().ToString() ?? "NULL")
                    .ConfigureAwait(false);

                session = session ?? new WebSession();

                // Save the latest session to bot's settings
                BotSettings.Session = session;
                BotSettings.SaveSettings();

                // Update authenticator session
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is IAuthenticatorBot authenticatorController)
                {
                    await BotLogger.Debug(nameof(OnNewWebSessionAvailable), "Updating IAuthenticatorBot's session.")
                        .ConfigureAwait(false);
                    authenticatorController.BotAuthenticatorSettings?.Authenticator?.Session?.UpdateSession(session);
                    authenticatorController.BotAuthenticatorSettings?.SaveSettings();
                }

                // Create a SteamWebAccess if missing or update the current SteamWebAccess
                if (WebAccess == null)
                {
                    await BotLogger.Debug(nameof(OnNewWebSessionAvailable), "Initializing an instance of WebAccess.")
                        .ConfigureAwait(false);
                    WebAccess = new SteamWebAccess(
                        session,
                        IPAddress.TryParse(BotSettings.PublicIPAddress, out var ipAddress) ? ipAddress : IPAddress.Any,
                        string.IsNullOrWhiteSpace(BotSettings.Proxy) ? null : new WebProxy(BotSettings.Proxy)
                    );
                }
                else
                {
                    await BotLogger.Debug(nameof(OnNewWebSessionAvailable), "Updating WebAccess's session.")
                        .ConfigureAwait(false);
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
                            await BotLogger.Debug(nameof(OnNewWebSessionAvailable),
                                    "API key is missing, retriving directly from Steam.")
                                .ConfigureAwait(false);
                            await backoff.Delay().ConfigureAwait(false);
                            apiKey = await SteamWebAPI.GetApiKey(WebAccess)
                                .ConfigureAwait(false);

                            if (string.IsNullOrWhiteSpace(apiKey))
                            {
                                await BotLogger.Debug(nameof(OnNewWebSessionAvailable),
                                    "Account not registered for an API key.").ConfigureAwait(false);

                                var domainName =
                                    !string.IsNullOrWhiteSpace(BotSettings.DomainName)
                                        ? BotSettings.DomainName
                                        : ((await (WebAccess ?? SteamWebAccess.GetGuest()).GetActualIPAddress()
                                               .ConfigureAwait(false))?.ToString() ??
                                           "example.com");

                                await BotLogger.Debug(nameof(OnNewWebSessionAvailable),
                                        "Registering for a new API key. DomainName = `{0}`", domainName)
                                    .ConfigureAwait(false);

                                if (await SteamWebAPI
                                    .RegisterApiKey(WebAccess, domainName)
                                    .ConfigureAwait(false))
                                {
                                    await BotLogger.Debug(nameof(OnNewWebSessionAvailable), "API key registered.")
                                        .ConfigureAwait(false);
                                }
                                else
                                {
                                    await BotLogger.Debug(nameof(OnNewWebSessionAvailable),
                                        "Failed to register API key. Another try in 30 seconds.").ConfigureAwait(false);
                                    await Task.Delay(TimeSpan.FromSeconds(30), CancellationTokenSource.Token)
                                        .ConfigureAwait(false);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            await BotLogger.Warning(nameof(OnNewWebSessionAvailable), e.Message).ConfigureAwait(false);
                            // ignored
                        }
                    }

                    // If API key acquired, save to bot's settings
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        await BotLogger.Debug(nameof(OnNewWebSessionAvailable),
                            "Saving API key to bot's settings.").ConfigureAwait(false);
                        BotSettings.ApiKey = apiKey;
                        BotSettings.SaveSettings();
                    }
                }

                // If API key is available, create or update the SteamWebAPI
                if (!string.IsNullOrWhiteSpace(BotSettings.ApiKey))
                {
                    if (WebAPI == null)
                    {
                        await BotLogger.Debug(nameof(OnNewWebSessionAvailable), "Initializing an instance of WebAPI.")
                            .ConfigureAwait(false);
                        WebAPI = new SteamWebAPI(
                            BotSettings.ApiKey,
                            WebAccess);
                    }
                    else
                    {
                        await BotLogger.Debug(nameof(OnNewWebSessionAvailable), "Updating WebAPI's APIKey.")
                            .ConfigureAwait(false);
                        WebAPI.ApiKey = BotSettings.ApiKey;
                    }
                }

                // Create a TradeOfferManager for classes implementing ITradeOfferController
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is ITradeOfferBot offerController)
                {
                    if (offerController.TradeOfferManager == null)
                    {
                        await BotLogger.Debug(nameof(OnNewWebSessionAvailable),
                            "Initializing an instance of TradeOfferManager for ITradeOfferBot.").ConfigureAwait(false);
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
                        offerController.TradeOfferManager.TradeOfferNeedsConfirmation +=
                            OnInternalTradeOfferNeedsConfirmation;
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
                        await BotLogger
                            .Debug(nameof(OnNewWebSessionAvailable),
                                "Initializing an instance of TradeManager for ITradeBot.")
                            .ConfigureAwait(false);
                        tradeController.TradeManager = new TradeManager(
                            WebAPI,
                            WebAccess,
                            tradeController.TradeBotSettings.TradeOptions ?? TradeOptions.Default
                        );
                        tradeController.TradeManager.TradeCreated += OnInternalTradeCreated;
                    }
                }

                await BotLogger.Debug(nameof(OnNewWebSessionAvailable), "Session updated successfully.")
                    .ConfigureAwait(false);

                lock (this)
                {
                    // If bot already logged it and this is a session update, just make sure bot is running
                    if (BotStatus != SteamBotStatus.LoggingIn)
                    {
                        BotStatus = SteamBotStatus.Running;

                        return;
                    }
                }

                await BotLogger.Debug(nameof(OnNewWebSessionAvailable), "Initializing Session Check Timer.")
                    .ConfigureAwait(false);

                // If this is an actual login, start the session check timer and change the bot's status
                SessionCheckTimer = new Timer(async state =>
                {
                    await OnCheckSession().ConfigureAwait(false);

                    if (CancellationTokenSource?.IsCancellationRequested == false)
                    {
                        SessionCheckTimer?.Change(TimeSpan.FromSeconds(BotSettings.SessionCheckInterval),
                            TimeSpan.FromMilliseconds(-1));
                    }
                    else
                    {
                        await OnTerminate().ConfigureAwait(false);
                    }
                }, null, TimeSpan.FromSeconds(BotSettings.SessionCheckInterval), TimeSpan.FromMilliseconds(-1));

                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is IAuthenticatorBot authenticatorBot)
                {
                    await BotLogger.Debug(nameof(OnNewWebSessionAvailable),
                        "Initializing IAuthenticatorBot's Authenticator Confirmation Timer.").ConfigureAwait(false);
                    AuthenticatorConfirmationTimer = new Timer(async state =>
                        {
                            await OnAuthenticatorCheckConfirmations().ConfigureAwait(false);

                            if (CancellationTokenSource?.IsCancellationRequested == false)
                            {
                                AuthenticatorConfirmationTimer?.Change(
                                    TimeSpan.FromSeconds(
                                        authenticatorBot.BotAuthenticatorSettings.ConfirmationCheckInterval),
                                    TimeSpan.FromMilliseconds(-1));
                            }
                        }, null,
                        TimeSpan.FromSeconds(authenticatorBot.BotAuthenticatorSettings.ConfirmationCheckInterval),
                        TimeSpan.FromMilliseconds(-1));
                }

                lock (this)
                {
                    BotStatus = SteamBotStatus.Running;
                }

                await BotLogger.Debug(nameof(OnNewWebSessionAvailable), "Logged in successfully.")
                    .ConfigureAwait(false);
                await OnLoggedIn().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // On failure, terminate the bot
                await BotLogger.Error(nameof(OnNewWebSessionAvailable), e.Message).ConfigureAwait(false);
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

        protected virtual async Task OnTerminate()
        {
            await BotLogger.Error(nameof(OnTerminate), "Terminating due to a fault. See previous logs.")
                .ConfigureAwait(false);

            lock (this)
            {
                BotStatus = SteamBotStatus.Faulted;
            }

            await StopBot().ConfigureAwait(false);
        }

        private async void OnInternalOfferDeclined(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            await BotLogger.Debug(nameof(OnInternalOfferDeclined),
                    "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                    tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId)
                .ConfigureAwait(false);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferDeclined(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalOfferInEscrow(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            await BotLogger.Debug(nameof(OnInternalOfferInEscrow),
                    "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                    tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId)
                .ConfigureAwait(false);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferInEscrow(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalTradeCreated(object sender, TradeCreatedEventArgs tradeCreatedEventArgs)
        {
            await BotLogger.Debug(nameof(OnInternalTradeCreated),
                    "TradeCreatedEventArgs.PartnerSteamId = `{0}`",
                    tradeCreatedEventArgs.PartnerSteamId)
                .ConfigureAwait(false);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeBot)?.OnTradeCreated(tradeCreatedEventArgs.PartnerSteamId,
                tradeCreatedEventArgs.Trade)).ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferAccepted(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            await BotLogger.Debug(nameof(OnInternalTradeOfferAccepted),
                    "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                    tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId)
                .ConfigureAwait(false);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferAccepted(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferCanceled(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            await BotLogger.Debug(nameof(OnInternalTradeOfferCanceled),
                    "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                    tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId)
                .ConfigureAwait(false);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferCanceled(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferChanged(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            await BotLogger.Debug(nameof(OnInternalTradeOfferChanged),
                    "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                    tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId)
                .ConfigureAwait(false);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferChanged(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferNeedsConfirmation(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            await BotLogger.Debug(nameof(OnInternalTradeOfferNeedsConfirmation),
                    "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                    tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId)
                .ConfigureAwait(false);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferNeedsConfirmation(tradeOfferStateChangedEventArgs
                .TradeOffer)).ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferReceived(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            await BotLogger.Debug(nameof(OnInternalTradeOfferReceived),
                    "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                    tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId)
                .ConfigureAwait(false);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferReceived(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferSent(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            await BotLogger.Debug(nameof(OnInternalTradeOfferSent),
                    "TradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId = `{0}`",
                    tradeOfferStateChangedEventArgs.TradeOffer.TradeOfferId)
                .ConfigureAwait(false);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferSent(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }
    }
}