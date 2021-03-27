using System;
using System.Threading.Tasks;
using ConsoleUtilities;
using Microsoft.Extensions.Logging;
using SDroid;
using SteamKit2;

namespace SDroidTest
{
    internal class SteamKitBot : SDroid.SteamKitBot
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public SteamKitBot(SteamKitBotSettings settings, ILogger botLogger) : base(settings, botLogger)
        {
            SubscribedCallbacks.Add(
                CallbackManager.Subscribe<SteamFriends.FriendMsgCallback>(OnSteamFriendsMessage)
            );
        }

        public new SteamKitBotSettings BotSettings
        {
            get => base.BotSettings as SteamKitBotSettings;
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
            BotLogger.LogInformation("Changing state to Online.");
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
            ConsoleWriter.Default.WriteObject(
                new
                {
                    walletInfo.Balance,
                    walletInfo.Currency,
                    walletInfo.HasWallet
                }
            );

            return Task.CompletedTask;
        }

        private void OnSteamFriendsMessage(SteamFriends.FriendMsgCallback friendMsgCallback)
        {
            switch (friendMsgCallback.EntryType)
            {
                case EChatEntryType.ChatMsg:
                    BotLogger.LogInformation(
                        "New message from {0}. Message = {1}",
                        friendMsgCallback.Sender,
                        friendMsgCallback.Message
                    );
                    break;
                case EChatEntryType.HistoricalChat:
                    BotLogger.LogInformation(
                        "Historic message from {0}. Message = {1}",
                        friendMsgCallback.Sender,
                        friendMsgCallback.Message
                    );
                    break;
                case EChatEntryType.InviteGame:
                    BotLogger.LogInformation(
                            "Invited to game by {0}. Message = {1}",
                            friendMsgCallback.Sender,
                            friendMsgCallback.Message
                        );
                    break;
                default:
                    BotLogger.LogInformation(
                        "Chat event by {0}. EChatEntryType = {1}",
                        friendMsgCallback.Sender,
                        friendMsgCallback.EntryType
                    );
                    break;
            }
        }
    }
}