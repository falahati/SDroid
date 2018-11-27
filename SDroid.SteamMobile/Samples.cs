using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Exceptions;

namespace SDroid.SteamMobile
{
    internal static class Samples
    {

        public static async Task RevokeAuthenticator()
        {
            var authenticator = Authenticator.DeSerializeFromFile("MyAuthenticator.maFile2");

            await authenticator.RevokeAuthenticator();
        }

        public static async Task GetSteamGuardCode1()
        {
            var authenticator = Authenticator.DeSerializeFromFile("MyAuthenticator.maFile2");

            var steamGuardCode = await authenticator.GenerateSteamGuardCode();
            // Do something with Steam Guard code
        }

        public static async Task GetSteamGuardCode2()
        {
            var authenticatorSharedSecret = "AUTHENTICATOR SHARED SECRED";

            // Authenticator shared secret is also available as part of a valid Authenticator instance
            //authenticatorSharedSecret = authenticator.AuthenticatorData.SharedSecret;

            var steamGuardCode = await Authenticator.GenerateSteamGuardCode(authenticatorSharedSecret);
            // Do something with Steam Guard code
        }

        public static async Task Confirmations()
        {
            var authenticator = Authenticator.DeSerializeFromFile("MyAuthenticator.maFile2");

            var confirmations = await authenticator.FetchConfirmations();

            foreach (var confirmation in confirmations)
            {
                switch (confirmation.Type)
                {
                    case ConfirmationType.Trade:
                        var tradeOfferId = confirmation.Creator;
                        // Should be cross-matched with a trade offer before deciding to confirm or deny

                        if (await authenticator.AcceptConfirmation(confirmation))
                        {
                            // Trade offer confirmed
                        }
                        else
                        {
                            // Failed to confirm the trade offer
                        }
                        break;
                    case ConfirmationType.MarketSellTransaction:
                        var marketTransactionId = confirmation.Creator;
                        // Should be cross-matched with a market sell transaction before deciding to confirm or deny

                        if (await authenticator.DenyConfirmation(confirmation))
                        {
                            // Market transaction denied
                        }
                        else
                        {
                            // Failed to deny the market transaction
                        }
                        break;
                }
            }
        }


        public static async Task RefreshSession()
        {
            var json = "SESSION JSON CONTENT";
            var mobileSession = JsonConvert.DeserializeObject<MobileSession>(json);

            if (mobileSession?.HasEnoughInfo() == true)
            {
                var webAccess = new SteamMobileWebAccess(mobileSession);

                if (await webAccess.VerifySession())
                {
                    // Session is valid
                }
                else
                {
                    // Session expired, lets try refreshing it
                    if (await mobileSession.RefreshSession(webAccess))
                    {
                        // Session refreshed and valid again
                    }
                    else
                    {
                        // Failed to refresh the session, must log in again
                    }
                }
            }
        }

        public static async Task AddPhoneToAccount()
        {
            var json = "LOGGED IN SESSION JSON CONTENT";
            var mobileSession = JsonConvert.DeserializeObject<MobileSession>(json);
            var webAccess = new SteamMobileWebAccess(mobileSession);
            var authenticatorLinker = new AuthenticatorLinker(webAccess);

            if (await authenticatorLinker.DoesAccountHasPhoneNumber())
            {
                // The account already has a phone number
                return;
            }

            var phoneNumber = "+98000000000";

            if (!await authenticatorLinker.RequestToAddPhoneNumber(phoneNumber))
            {
                // Request to add the phone number to account failed
                return;
            }
            
            // To accept and finalize this authenticator we need to have hold of the
            // text message sent to the account's associated phone number
            var smsCode = "SMS CODE HERE";

            if (!await authenticatorLinker.VerifyPhoneNumberBySMS(smsCode))
            {
                // Failed to finalize and verify added phone number
                // Probably bad SMS code; should try again
                return;
            }
        }

        public static async Task LinkAuthenticator()
        {
            var json = "LOGGED IN SESSION JSON CONTENT";
            var mobileSession = JsonConvert.DeserializeObject<MobileSession>(json);
            var webAccess = new SteamMobileWebAccess(mobileSession);
            var authenticatorLinker = new AuthenticatorLinker(webAccess);

            var authenticator = await authenticatorLinker.RequestToAddAuthenticator();

            // We just got a valid authenticator data
            // We should serialize and save this instance before doing anything else
            authenticator.SerializeToFile("MyAuthenticator.maFile2");

            // To accept and finalize this authenticator we need to have hold of the
            // text message sent to the account's associated phone number
            var smsCode = "SMS CODE HERE";

            try
            {
                await authenticatorLinker.FinalizeAddAuthenticator(authenticator, smsCode);

                // Authenticator finalized
            }
            catch (AuthenticatorLinkerException e)
            {

                if (e.ErrorCode == AuthenticatorLinkerErrorCode.BadSMSCode)
                {
                    // Bad SMS code
                    throw;
                }
            }
        }
    }
}
