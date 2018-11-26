using System;
using System.IO;
using System.Threading.Tasks;
using ConsoleUtilities;
using SDroid;
using SDroid.Interfaces;
using SDroid.SteamMobile;

namespace SDroidTest
{
    internal class AuthenticatorBot : SteamBot, IAuthenticatorBot
    {
        public AuthenticatorBot(AuthenticatorBotSettings settings, Logger logger) : base(settings, logger)
        {
        }

        public new AuthenticatorBotSettings BotSettings
        {
            get => base.BotSettings as AuthenticatorBotSettings;
        }

        /// <inheritdoc />
        public IAuthenticatorSettings BotAuthenticatorSettings
        {
            get => BotSettings;
        }

        /// <inheritdoc />
        public Task OnAuthenticatorConfirmationAvailable(Confirmation confirmation)
        {
            ConsoleWriter.Default.PrintMessage("New confirmation available.");
            ConsoleWriter.Default.WriteObject(confirmation);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task OnAuthenticatorMissing()
        {
            ConsoleWriter.Default.PrintWarning(
                "This account has authenticator linked to it. Provide us with the authentication file associated with this account. (*.*, *.maFile, *.maFile2)");

            while (true)
            {
                var maFileAddress = ConsoleWriter.Default.PrintQuestion("Authentication file address");

                if (File.Exists(maFileAddress))
                {
                    try
                    {
                        var authenticator = Authenticator.DeSerializeFromFile(maFileAddress);

                        if (authenticator != null && authenticator.HasEnoughInfo())
                        {
                            BotSettings.Authenticator = authenticator;
                            BotSettings.Username = authenticator.AuthenticatorData.AccountName;
                            BotSettings.SaveSettings();

                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        ConsoleWriter.Default.WriteException(e);
                    }
                }
                else
                {
                    ConsoleWriter.Default.PrintError("File not found.");
                }
            }

            return Task.CompletedTask;
        }

        public override async Task StartBot()
        {
            await base.StartBot().ConfigureAwait(false);

            await BotLogin().ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override Task<string> OnPasswordRequired()
        {
            return Task.FromResult(ConsoleWriter.Default.PrintQuestion("Password"));
        }
    }
}