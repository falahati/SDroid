using System.Threading.Tasks;
using ConsoleUtilities;
using SDroid;
using SDroid.Interfaces;
using SDroid.SteamTrade;

namespace SDroidTest
{
    public class TradeOfferBot : SteamBot, ITradeOfferBot
    {
        /// <inheritdoc />
        public TradeOfferBot(TradeOfferBotSettings settings, TradeOfferLogger botLogger) : base(settings, botLogger)
        {
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
            get => BotSettings as ITradeOfferBotSettings;
        }

        /// <inheritdoc />
        TradeOfferManager ITradeOfferBot.TradeOfferManager { get; set; }

        /// <inheritdoc />
        protected override Task<string> OnAuthenticatorCodeRequired()
        {
            return Task.FromResult(ConsoleWriter.Default.PrintQuestion("Enter 2FA Code"));
        }
    }
}