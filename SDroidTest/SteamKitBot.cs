using System.Threading.Tasks;
using ConsoleUtilities;
using SDroid;
using SteamKit2;

namespace SDroidTest
{
    internal class SteamKitBot : SDroid.SteamKitBot
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
            BotLogger.Info("OnLoggedIn", "Changing state to Online.");
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