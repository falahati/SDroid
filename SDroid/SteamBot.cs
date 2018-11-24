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

namespace SDroid
{
    // ReSharper disable once ClassTooBig
    public abstract class SteamBot
    {
        protected readonly SemaphoreSlim WebSessionLock = new SemaphoreSlim(1, 1);
        protected Timer AuthenticatorConfirmationTimer;
        protected List<Confirmation> FetchedConfirmations = new List<Confirmation>();
        protected Timer SessionCheckTimer;

        protected SteamBot(IBotSettings settings, IBotLogger botLogger)
        {
            BotSettings = settings;
            BotLogger = botLogger;
            BotStatus = SteamBotStatus.Ready;
        }

        public IBotLogger BotLogger { get; protected set; }

        public IBotSettings BotSettings { get; protected set; }
        public SteamBotStatus BotStatus { get; protected set; }
        protected CancellationTokenSource CancellationTokenSource { get; set; }
        protected SteamWebAccess WebAccess { get; set; }
        protected SteamWebAPI WebAPI { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{GetType()}] {BotSettings.Username} - {BotStatus}";
        }

        public virtual void Dispose()
        {
            StopBot().Wait();
            CancellationTokenSource?.Dispose();
        }

        public virtual Task StartBot()
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

            var _ = BotLogin();

            return Task.CompletedTask;
        }


        public virtual async Task StopBot()
        {
            CancellationTokenSource?.Cancel(true);
            SessionCheckTimer?.Dispose();
            AuthenticatorConfirmationTimer?.Dispose();

            while (true)
            {
                lock (this)
                {
                    if (BotStatus == SteamBotStatus.Faulted)
                    {
                        break;
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(300)).ConfigureAwait(false);
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ITradeOfferBot offerController)
            {
                lock (this)
                {
                    offerController.TradeOfferManager?.Dispose();
                    offerController.TradeOfferManager = null;
                }
            }

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this is ITradeBot tradeController)
            {
                lock (this)
                {
                    tradeController.TradeManager?.Dispose();
                    tradeController.TradeManager = null;
                }
            }

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

            try
            {
                // Check to see if we have a valid session saved
                if (BotSettings.Session != null && BotSettings.Session.HasEnoughInfo() && WebAccess == null)
                {
                    var webAccess = new SteamWebAccess(BotSettings.Session,
                        BotSettings.PublicIPAddress ?? IPAddress.Any, BotSettings.Proxy);

                    if (await webAccess.VerifySession().ConfigureAwait(false))
                    {
                        WebAccess = webAccess;
                        await OnNewWebSessionAvailable(WebAccess.Session).ConfigureAwait(false);

                        return;
                    }
                }

                // Check if the current session is still valid
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (WebAccess != null)
                {
                    if (await WebAccess.VerifySession().ConfigureAwait(false))
                    {
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

                    if (authenticatorController.BotAuthenticatorSettings?.Authenticator?.Session != null)
                    {
                        if (await
                            authenticatorController.BotAuthenticatorSettings.Authenticator.Session.RefreshSession()
                                .ConfigureAwait(false))
                        {
                            await OnNewWebSessionAvailable(
                                authenticatorController.BotAuthenticatorSettings.Authenticator.Session
                            ).ConfigureAwait(false);

                            return;
                        }
                    }
                }

                // If nothing found, start the login process with the WebLogin or MobileLogin
                await OnLoggingIn().ConfigureAwait(false);
                var loginCredentials = new LoginCredentials(BotSettings.Username, BotSettings.Password);
                var backoff = new ExponentialBackoff();

                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    try
                    {
                        await backoff.Delay().ConfigureAwait(false);
                        var session = await webLogin.DoLogin(loginCredentials).ConfigureAwait(false);
                        await OnNewWebSessionAvailable(session).ConfigureAwait(false);

                        return;
                    }
                    catch (UserLoginException e)
                    {
                        switch (e.ErrorCode)
                        {
                            case UserLoginErrorCode.GeneralFailure:

                                throw;
                            case UserLoginErrorCode.BadRSAResponse:

                                throw;
                            case UserLoginErrorCode.BadCredentials:

                                throw;
                            case UserLoginErrorCode.NeedsCaptchaCode:
                                loginCredentials.CaptchaCode =
                                    await OnCaptchaCodeRequired(await e.UserLogin.DownloadCaptchaImage()
                                            .ConfigureAwait(false))
                                        .ConfigureAwait(false);
                                backoff.Reset();

                                break;
                            case UserLoginErrorCode.NeedsTwoFactorAuthenticationCode:
                                loginCredentials.TwoFactorAuthenticationCode =
                                    await OnAuthenticatorCodeRequired().ConfigureAwait(false);
                                backoff.Reset();

                                break;
                            case UserLoginErrorCode.NeedsEmailVerificationCode:
                                loginCredentials.EmailVerificationCode =
                                    await OnEmailCodeRequired().ConfigureAwait(false);
                                backoff.Reset();

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
                await BotLogger.Error("Login", e.Message).ConfigureAwait(false);
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
                    await BotLogger.Error("OnAuthenticatorCheckConfirmations", e.Message).ConfigureAwait(false);
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
                    await authenticator.OnAuthenticatorMissing().ConfigureAwait(false);
                }

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

                if (!await WebAccess.VerifySession().ConfigureAwait(false))
                {
                    await BotLogger.Warning("OnCheckSession", "Session expired.").ConfigureAwait(false);

                    await BotLogin().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                await BotLogger.Error("OnCheckSession", e.Message).ConfigureAwait(false);
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

        protected virtual async Task OnNewWebSessionAvailable(WebSession session)
        {
            try
            {
                await WebSessionLock.WaitAsync().ConfigureAwait(false);

                session = session ?? new WebSession();

                // Save the latest session to bot's settings
                BotSettings.Session = session;
                BotSettings.SaveSettings();

                // Update authenticator session
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is IAuthenticatorBot authenticatorController)
                {
                    authenticatorController.BotAuthenticatorSettings?.Authenticator?.Session?.UpdateSession(session);
                    authenticatorController.BotAuthenticatorSettings?.SaveSettings();
                }

                // Create a SteamWebAccess if missing or update the current SteamWebAccess
                if (WebAccess == null)
                {
                    WebAccess = new SteamWebAccess(session, BotSettings.PublicIPAddress ?? IPAddress.Any,
                        BotSettings.Proxy);
                }
                else
                {
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
                            await backoff.Delay().ConfigureAwait(false);
                            apiKey = await SteamWebAPI.GetApiKey(WebAccess)
                                .ConfigureAwait(false);

                            if (apiKey == null)
                            {
                                var domainName =
                                    !string.IsNullOrWhiteSpace(BotSettings.DomainName)
                                        ? BotSettings.DomainName
                                        : ((await (WebAccess ?? SteamWebAccess.GetGuest()).GetActualIPAddress()
                                               .ConfigureAwait(false))?.ToString() ??
                                           "example.com");

                                while (!await SteamWebAPI
                                    .RegisterApiKey(WebAccess, domainName)
                                    .ConfigureAwait(false))
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            await BotLogger.Warning("OnNewWebSessionAvailable", e.Message).ConfigureAwait(false);
                            // ignored
                        }
                    }

                    // If API key acquired, save to bot's settings
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        BotSettings.ApiKey = apiKey;
                        BotSettings.SaveSettings();
                    }
                }

                // If API key is not available, create or update the SteamWebAPI
                if (!string.IsNullOrWhiteSpace(BotSettings.ApiKey))
                {
                    if (WebAPI == null)
                    {
                        WebAPI = new SteamWebAPI(
                            BotSettings.ApiKey,
                            WebAccess);
                    }
                    else
                    {
                        WebAPI.ApiKey = BotSettings.ApiKey;
                    }
                }

                // Create a TradeOfferManager for classes implementing ITradeOfferController
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (this is ITradeOfferBot offerController)
                {
                    if (offerController.TradeOfferManager == null)
                    {
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
                        tradeController.TradeManager = new TradeManager(
                            WebAPI,
                            WebAccess,
                            tradeController.TradeBotSettings.TradeOptions ?? TradeOptions.Default
                        );
                        tradeController.TradeManager.TradeCreated += OnInternalTradeCreated;
                    }
                }

                lock (this)
                {
                    // If bot already logged it and this is a session update, just make sure bot is running
                    if (BotStatus != SteamBotStatus.LoggingIn)
                    {
                        BotStatus = SteamBotStatus.Running;

                        return;
                    }
                }

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

                await OnLoggedIn().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // On failure, terminate the bot
                await BotLogger.Error("OnNewWebSessionAvailable", e.Message).ConfigureAwait(false);
                await OnTerminate().ConfigureAwait(false);
            }
            finally
            {
                WebSessionLock.Release();
            }
        }

        protected virtual async Task OnTerminate()
        {
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
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferDeclined(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalOfferInEscrow(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferInEscrow(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalTradeCreated(object sender, TradeCreatedEventArgs tradeCreatedEventArgs)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeBot)?.OnTradeCreated(tradeCreatedEventArgs.PartnerSteamId,
                tradeCreatedEventArgs.Trade)).ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferAccepted(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferAccepted(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferCanceled(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferCanceled(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferChanged(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferChanged(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferNeedsConfirmation(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferNeedsConfirmation(tradeOfferStateChangedEventArgs
                .TradeOffer)).ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferReceived(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferReceived(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }

        private async void OnInternalTradeOfferSent(
            object sender,
            TradeOfferStateChangedEventArgs tradeOfferStateChangedEventArgs)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once SuspiciousTypeConversion.Global
            await ((this as ITradeOfferBot)?.OnTradeOfferSent(tradeOfferStateChangedEventArgs.TradeOffer))
                .ConfigureAwait(false);
        }
    }
}