using System;
using System.Threading.Tasks;
using ConsoleUtilities;
using SDroid;
using SDroid.Interfaces;
using SteamKit2;

namespace SDroidTest
{
    internal class SteamKitBot : SDroid.SteamKitBot, ISteamKitChatBot
    {
        /// <inheritdoc />
        public SteamKitBot(SteamKitBotSettings settings, IBotLogger botLogger) : base(settings, botLogger)
        {
        }

        public new SteamKitBotSettings BotSettings
        {
            get => base.BotSettings as SteamKitBotSettings;
        }

        /// <inheritdoc />
        public async Task OnChatGameInvited(SteamID partnerSteamId, string message)
        {
            await BotLogger
                .Info(nameof(OnChatGameInvited), "Invited to game by {0}. Message = {1}", partnerSteamId, message)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task OnChatHistoricMessageReceived(SteamID partnerSteamId, string message)
        {
            await BotLogger.Info(nameof(OnChatHistoricMessageReceived), "Historic message from {0}. Message = {1}",
                partnerSteamId, message).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task OnChatMessageReceived(SteamID partnerSteamId, string message)
        {
            await BotLogger
                .Info(nameof(OnChatMessageReceived), "New message from {0}. Message = {1}", partnerSteamId, message)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task OnChatPartnerEvent(SteamID partnerSteamId, SteamKitChatPartnerEvent chatEvent)
        {
            await BotLogger.Info(nameof(OnChatPartnerEvent), "Chat event by {0}. SteamKitChatPartnerEvent = {1}",
                partnerSteamId, chatEvent).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task StartBot()
        {
            await base.StartBot().ConfigureAwait(false);


            while (BotStatus != SteamBotStatus.Running && BotStatus != SteamBotStatus.Faulted)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        protected override Task OnAccountInfoAvailable(SteamUser.AccountInfoCallback accountInfo)
        {
            ConsoleWriter.Default.WriteObject(new
            {
                accountInfo.AccountFlags,
                accountInfo.CountAuthedComputers,
                accountInfo.Country,
                accountInfo.FacebookID,
                accountInfo.FacebookName,
                accountInfo.PersonaName
            });

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task<string> OnAuthenticatorCodeRequired()
        {
            return Task.FromResult(ConsoleWriter.Default.PrintQuestion("Steam Guard Code"));
        }

        /// <inheritdoc />
        protected override Task OnConnected()
        {
            return BotLogin();
        }

        /// <inheritdoc />
        protected override Task OnLoggedIn()
        {
            BotLogger.Info(nameof(OnLoggedIn), "Changing state to Online.");
            SteamFriends.SetPersonaState(EPersonaState.Online);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task<string> OnPasswordRequired()
        {
            return Task.FromResult(ConsoleWriter.Default.PrintQuestion("Password"));
        }

        /// <inheritdoc />
        protected override Task OnWalletInfoAvailable(SteamUser.WalletInfoCallback walletInfo)
        {
            ConsoleWriter.Default.WriteObject(new
            {
                walletInfo.Balance,
                walletInfo.Currency,
                walletInfo.HasWallet
            });

            return Task.CompletedTask;
        }
    }
}