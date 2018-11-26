using System.Threading.Tasks;
using ConsoleUtilities;
using SDroid;
using SDroid.Interfaces;
using SDroid.SteamTrade;

namespace SDroidTest
{
    public class TradeOfferBot : SteamBot, ITradeOfferBot
    {
        private UserInventory _myInventory;

        /// <inheritdoc />
        public TradeOfferBot(TradeOfferBotSettings settings, Logger botLogger) : base(settings, botLogger)
        {
        }

        public new TradeOfferBotSettings BotSettings
        {
            get => base.BotSettings as TradeOfferBotSettings;
        }

        /// <inheritdoc />
        public Task OnTradeOfferAccepted(TradeOffer tradeOffer)
        {
            BotLogger.Info("TradeOfferAccepted", tradeOffer.TradeOfferId.ToString());

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferCanceled(TradeOffer tradeOffer)
        {
            BotLogger.Info("TradeOfferCanceled", tradeOffer.TradeOfferId.ToString());

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferChanged(TradeOffer tradeOffer)
        {
            BotLogger.Info("TradeOfferChanged", tradeOffer.TradeOfferId.ToString());

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferDeclined(TradeOffer tradeOffer)
        {
            BotLogger.Info("TradeOfferDeclined", tradeOffer.TradeOfferId.ToString());

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferInEscrow(TradeOffer tradeOffer)
        {
            BotLogger.Info("TradeOfferInEscrow", tradeOffer.TradeOfferId.ToString());

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferNeedsConfirmation(TradeOffer tradeOffer)
        {
            BotLogger.Info("TradeOfferNeedsConfirmation", tradeOffer.TradeOfferId.ToString());

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferReceived(TradeOffer tradeOffer)
        {
            BotLogger.Info("TradeOfferReceived", tradeOffer.TradeOfferId.ToString());

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnTradeOfferSent(TradeOffer tradeOffer)
        {
            BotLogger.Info("TradeOfferSent", tradeOffer.TradeOfferId.ToString());

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public ITradeOfferBotSettings TradeOfferBotSettings
        {
            get => BotSettings;
        }

        /// <inheritdoc />
        public override async Task StartBot()
        {
            await base.StartBot().ConfigureAwait(false);

            await BotLogin().ConfigureAwait(false);
        }

        /// <inheritdoc />
        TradeOfferManager ITradeOfferBot.TradeOfferManager { get; set; }

        /// <inheritdoc />
        protected override Task<string> OnAuthenticatorCodeRequired()
        {
            return Task.FromResult(ConsoleWriter.Default.PrintQuestion("Steam Guard Code"));
        }

        /// <inheritdoc />
        protected override async Task OnLoggedIn()
        {
            await BotLogger.Info("OnLoggedIn", "Retrieving bot's inventory.").ConfigureAwait(false);

            if (_myInventory == null)
            {
                _myInventory = await UserInventory.GetInventory(WebAccess, SteamId).ConfigureAwait(false);
            }

            _myInventory.ClearCache();

            var assets = await _myInventory.GetAssets().ConfigureAwait(false);

            await BotLogger.Info("OnLoggedIn", "{0} assets found on bot's inventory.", assets.Length)
                .ConfigureAwait(false);

            await base.OnLoggedIn().ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override Task<string> OnPasswordRequired()
        {
            return Task.FromResult(ConsoleWriter.Default.PrintQuestion("Password"));
        }
    }
}