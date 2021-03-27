using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleUtilities;
using Microsoft.Extensions.Logging;
using SDroid;
using SDroid.Interfaces;
using SDroid.SteamMobile;

namespace SDroidTest
{
    internal class AuthenticatorBot : SteamBot, IAuthenticatorBot
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public AuthenticatorBot(AuthenticatorBotSettings settings, ILogger logger) : base(settings, logger)
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

            if (BotStatus != SteamBotStatus.Running)
            {
                await BotLogin().ConfigureAwait(false);
            }
        }

        public async Task ConfirmByTradeOfferId(long tradeOfferId)
        {
            try
            {
                var authenticator = (this as IAuthenticatorBot).BotAuthenticatorSettings?.Authenticator;

                if (authenticator != null)
                {
                    var confirmations = await authenticator.FetchConfirmations().ConfigureAwait(false);

                    var confirmation = confirmations?.FirstOrDefault(c =>
                        c.Type == ConfirmationType.Trade && c.Creator == (ulong) tradeOfferId);

                    if (confirmation != null)
                    {
                        await authenticator.AcceptConfirmation(confirmation).ConfigureAwait(false);
                        BotLogger.LogInformation("Requested to confirm trade offer #{0}", tradeOfferId);

                        return;
                    }
                }

                BotLogger.LogInformation("Confirmation for trade offer #{0} not found.", tradeOfferId);
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, e.Message);
            }
        }

        public async Task DenyByTradeOfferId(long tradeOfferId)
        {
            try
            {
                var authenticator = (this as IAuthenticatorBot).BotAuthenticatorSettings?.Authenticator;

                if (authenticator != null)
                {
                    var confirmations = await authenticator.FetchConfirmations().ConfigureAwait(false);

                    var confirmation = confirmations?.FirstOrDefault(c =>
                        c.Type == ConfirmationType.Trade && c.Creator == (ulong) tradeOfferId);

                    if (confirmation != null)
                    {
                        await authenticator.DenyConfirmation(confirmation).ConfigureAwait(false);
                        BotLogger.LogInformation(nameof(DenyByTradeOfferId), "Requested to deny trade offer #{0}", tradeOfferId);

                        return;
                    }
                }

                BotLogger.LogInformation("Confirmation for trade offer #{0} not found.", tradeOfferId);
            }
            catch (Exception e)
            {
                BotLogger.LogError(e, e.Message);
            }
        }

        /// <inheritdoc />
        protected override Task<string> OnPasswordRequired()
        {
            return Task.FromResult(ConsoleWriter.Default.PrintQuestion("Password"));
        }
    }
}