using System;
using System.Threading.Tasks;
using ConsoleUtilities;
using Microsoft.Extensions.Logging;
using SDroid;
using SDroid.Interfaces;
using SDroid.SteamTrade;

namespace SDroidTest
{
    // ReSharper disable once InconsistentNaming
    internal class TradeOfferBot : SteamBot, ITradeOfferBot
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public TradeOfferBot(TradeOfferBotSettings settings, ILogger botLogger) : base(settings, botLogger)
        {
        }

        public new TradeOfferBotSettings BotSettings
        {
            get => base.BotSettings as TradeOfferBotSettings;
        }

        public UserInventory MyInventory { get; protected set; }

        /// <inheritdoc />
        public Task OnTradeOfferAccepted(TradeOffer tradeOffer)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferCanceled(TradeOffer tradeOffer)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferChanged(TradeOffer tradeOffer)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferDeclined(TradeOffer tradeOffer)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferInEscrow(TradeOffer tradeOffer)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferNeedsConfirmation(TradeOffer tradeOffer)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferReceived(TradeOffer tradeOffer)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferSent(TradeOffer tradeOffer)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public ITradeOfferBotSettings TradeOfferBotSettings
        {
            get => BotSettings;
        }

        /// <inheritdoc />
        TradeOfferManager ITradeOfferBot.TradeOfferManager { get; set; }

        /// <inheritdoc />
        public override async Task StartBot()
        {
            await base.StartBot().ConfigureAwait(false);

            if (BotStatus != SteamBotStatus.Running)
            {
                await BotLogin().ConfigureAwait(false);
            }
        }

        public async Task AcceptTradeOfferById(long tradeOfferId)
        {
            try
            {
                var tradeOfferManager = (this as ITradeOfferBot).TradeOfferManager;
                var tradeOffer = await tradeOfferManager.GetTradeOffer(tradeOfferId).ConfigureAwait(false);

                if (tradeOffer != null)
                {
                    await tradeOfferManager.Accept(tradeOffer).ConfigureAwait(false);
                    BotLogger.LogInformation("Requested to accept trade offer #{0}", tradeOffer.TradeOfferId);
                }
                else
                {
                    BotLogger.LogInformation("Trade offer #{0} not found.", tradeOfferId);
                }
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, e.Message);
            }
        }

        public async Task DeclineTradeOfferById(long tradeOfferId)
        {
            try
            {
                var tradeOfferManager = (this as ITradeOfferBot).TradeOfferManager;
                var tradeOffer = await tradeOfferManager.GetTradeOffer(tradeOfferId).ConfigureAwait(false);

                if (tradeOffer != null)
                {
                    await tradeOfferManager.Decline(tradeOffer).ConfigureAwait(false);
                    BotLogger.LogInformation("Requested to decline trade offer #{0}", tradeOffer.TradeOfferId);
                }
                else
                {
                    BotLogger.LogInformation("Trade offer #{0} not found.", tradeOfferId);
                }
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, e.Message);
            }
        }

        /// <inheritdoc />
        protected override Task<string> OnAuthenticatorCodeRequired()
        {
            return Task.FromResult(ConsoleWriter.Default.PrintQuestion("Steam Guard Code"));
        }

        /// <inheritdoc />
        protected override async Task OnLoggedIn()
        {
            BotLogger.LogInformation("Retrieving bot's inventory.");

            if (MyInventory == null)
            {
                MyInventory = await UserInventory.GetInventory(WebAccess, SteamId).ConfigureAwait(false);
            }

            MyInventory.ClearCache();

            //var assets = await MyInventory.GetAssets().ConfigureAwait(false);

            //await BotLogger.Info(nameof(OnLoggedIn), "{0} assets found in bot's inventory.", assets.Length)
            //    .ConfigureAwait(false);

            await base.OnLoggedIn().ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override Task<string> OnPasswordRequired()
        {
            return Task.FromResult(ConsoleWriter.Default.PrintQuestion("Password"));
        }
    }
}