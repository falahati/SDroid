using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamWeb.Exceptions;

namespace SDroid.SteamWeb
{
    internal static class Samples
    {
        public static async Task Login()
        {
            var username = "USERNAME";
            var password = "PASSWORD";

            var loginCredentials = new LoginCredentials(username, password);
            var webLogin = new WebLogin();

            while (true)
            {
                try
                {
                    var session = await webLogin.DoLogin(loginCredentials);

                    if (session?.HasEnoughInfo() == true)
                    {
                        // Login complete, serialize session or create an instance of SteamWebAccess

                        return;
                    }
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
                        case UserLoginErrorCode.TooManyFailedLoginAttempts:

                            throw;
                        case UserLoginErrorCode.NeedsCaptchaCode:
                            // Should download the captcha image and fill the CaptchaCode property of LoginCredentials and try again
                            var captchaImage = await e.UserLogin.DownloadCaptchaImage();
                            loginCredentials.CaptchaCode = "CAPTCHA CODE";

                            break;
                        case UserLoginErrorCode.NeedsTwoFactorAuthenticationCode:
                            // This account has a mobile authenticator associated with it
                            // Should fill the TwoFactorAuthenticationCode property of LoginCredentials and try again
                            loginCredentials.TwoFactorAuthenticationCode = "AUTHENTICATOR 2FA CODE";

                            break;
                        case UserLoginErrorCode.NeedsEmailVerificationCode:
                            // This account uses Steam Guard for added security
                            // Should fill the EmailVerificationCode property of LoginCredentials and try again
                            loginCredentials.EmailVerificationCode = "EMAIL VERIFICATION CODE";

                            break;
                    }
                }
            }
        }

        public static async Task VerifySession()
        {
            var json = "SESSION JSON CONTENT";
            var webSession = JsonConvert.DeserializeObject<WebSession>(json);

            if (webSession?.HasEnoughInfo() == true)
            {
                var webAccess = new SteamWebAccess(webSession);

                if (await webAccess.VerifySession())
                {
                    // Session is valid
                }
                else
                {
                    // Session expired, please login again
                }
            }
        }


        public static async Task APIRequest()
        {
            var apiKey = "API KEY";
            var json = "SESSION JSON CONTENT";
            var webSession = JsonConvert.DeserializeObject<WebSession>(json);

            if (webSession?.HasEnoughInfo() == true)
            {
                var webAccess = new SteamWebAccess(webSession);
                var webAPI = new SteamWebAPI(apiKey, webAccess);

                var tf2News = await webAPI.RequestDynamic(
                    "ISteamNews",
                    SteamWebAccessRequestMethod.Get,
                    "GetNewsForApp",
                    "v2",
                    new
                    {
                        appid = 440
                    }
                );

                // Show tf2News
            }
        }

        public static async Task GetAPIKey()
        {
            var json = "SESSION JSON CONTENT";
            var webSession = JsonConvert.DeserializeObject<WebSession>(json);

            if (webSession?.HasEnoughInfo() == true)
            {
                var webAccess = new SteamWebAccess(webSession);

                var apiKey = await SteamWebAPI.GetApiKey(webAccess);

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    if (await SteamWebAPI.RegisterApiKey(webAccess, "www.example.com"))
                    {
                        apiKey = await SteamWebAPI.GetApiKey(webAccess);
                    }
                    else
                    {
                        // Failed to register a new API key for this account
                    }
                }

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    // Retrieved a valid API key associated with the account represented by the passed WebSession
                    // Use this API key to create an instance of SteamWebAPI or just save it somewhere for later use

                    var webAPI = new SteamWebAPI(apiKey, webAccess);
                }
            }
        }
    }
}
