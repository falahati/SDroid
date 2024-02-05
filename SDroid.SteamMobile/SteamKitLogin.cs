using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamMobile.InternalModels;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Exceptions;
using SDroid.SteamWeb.Models;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;

namespace SDroid.SteamMobile
{
    /// <summary>
    ///     Handles logging the user into the mobile Steam website. Necessary to generate OAuth token and session cookies.
    /// </summary>
    public class SteamKitLogin : WebLogin, IAuthenticator
    {
        private const string LoginSteamPoweredBaseUrl = "https://login.steampowered.com";
        private string FinalizeLoginUrl => $"{LoginSteamPoweredBaseUrl}/jwt/finalizelogin";
        private string lastTwoFactorCodeProvided = "";
        private string lastEmailCodeProvided = "";


        /// <summary>
        ///     Tries to authenticate a user with the provided user credentials and returns session data corresponding to a
        ///     successful login; fails if information provided is not enough or service is unavailable.
        /// </summary>
        /// <param name="credentials">The credentials to be used for login process.</param>
        /// <returns>Logged in session to be used with other classes</returns>
        /// <exception cref="ArgumentException">
        ///     Username and/or password is missing. - credentials
        ///     or
        ///     Two factor authentication code is required for login process to continue. - credentials
        ///     or
        ///     Email verification code is required for login process to continue. - credentials
        ///     or
        ///     Captcha is required for login process to continue. - credentials
        /// </exception>
        /// <exception cref="UserLoginException">
        ///     Raises when there is a problem with login process or there is a need for more information. Capture and decide if
        ///     you should repeat the process.
        /// </exception>
        public override async Task<WebSession> DoLogin(LoginCredentials credentials)
        {
            if (string.IsNullOrWhiteSpace(credentials.UserName) || string.IsNullOrWhiteSpace(credentials.Password))
            {
                throw new ArgumentException("Username and/or password is missing.", nameof(credentials));
            }

            if (RequiresTwoFactorAuthenticationCode &&
                string.IsNullOrWhiteSpace(credentials.TwoFactorAuthenticationCode))
            {
                throw new ArgumentException("Two factor authentication code is required for login process to continue.",
                    nameof(credentials));
            }

            if (RequiresEmailVerification && string.IsNullOrWhiteSpace(credentials.EmailVerificationCode))
            {
                throw new ArgumentException("Email verification code is required for login process to continue.",
                    nameof(credentials));
            }

            if (RequiresCaptchaCode && string.IsNullOrWhiteSpace(credentials.CaptchaCode))
            {
                throw new ArgumentException("Captcha is required for login process to continue.", nameof(credentials));
            }

            // Lock this instance
            await LockObject.WaitAsync().ConfigureAwait(false);

            try
            {
                // Retrieve guest cookies for login process if missing
                if (string.IsNullOrEmpty(SteamWebAccess?.Session?.SessionId))
                {
                    await GetGuestSession().ConfigureAwait(false);
                }

                // Start a new SteamClient instance
                var steamClient = new SteamClient();

                // Connect to Steam
                steamClient.Connect();

                // Really basic way to wait until Steam is connected
                while (!steamClient.IsConnected)
                    await Task.Delay(500);


                lastEmailCodeProvided = credentials.EmailVerificationCode;
                lastTwoFactorCodeProvided = credentials.TwoFactorAuthenticationCode;

                // Create a new auth session
                CredentialsAuthSession authSession;
                try
                {
                    authSession = await steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
                    {
                        Username = credentials.UserName,
                        Password = credentials.Password,
                        IsPersistentSession = false,
                        PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp,
                        ClientOSType = EOSType.Android9,
                        Authenticator = this,
                    });
                }
                catch (Exception)
                {
                    steamClient.Disconnect();
                    throw new UserLoginException(UserLoginErrorCode.BadCredentials, this);
                }

                // Starting polling Steam for authentication response
                AuthPollResult pollResponse;
                try
                {
                    pollResponse = await authSession.PollingWaitForResultAsync();
                }
                catch (Exception ex)
                {
                    steamClient.Disconnect();
                    if (ex is UserLoginException)
                    {
                        throw;
                    }

                    throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
                }

                var sessionDate = new MobileSession(
                    authSession.SteamID,
                    null,
                    null,
                    pollResponse.AccessToken,
                    pollResponse.RefreshToken
                    );

                if (!sessionDate.HasEnoughInfo())
                {
                    throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
                }

                ResetStates();
                return sessionDate;
            }
            finally
            {
                // Unlock this instance
                LockObject.Release();
            }
        }

        protected override async Task<bool> GetGuestSession()
        {
            // Get a new SessionId
            SteamWebAccess = SteamMobileWebAccess.GetGuest();

            (
                await OperationRetryHelper.Default
                    .RetryOperationAsync(() =>
                        SteamWebAccess.FetchBinary(new SteamWebAccessRequest(SteamWebAccess.CommunityBaseUrl)))
                    .ConfigureAwait(false)
            ).Dispose();

            (
                await OperationRetryHelper.Default
                    .RetryOperationAsync(() =>
                        SteamWebAccess.FetchBinary(new SteamWebAccessRequest(LoginSteamPoweredBaseUrl)))
                    .ConfigureAwait(false)
            ).Dispose();

            return !string.IsNullOrWhiteSpace(SteamWebAccess?.Session?.SessionId);
        }

        public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        {
            if (previousCodeWasIncorrect || string.IsNullOrWhiteSpace(lastTwoFactorCodeProvided))
            {
                this.lastTwoFactorCodeProvided = "";
                throw new UserLoginException(UserLoginErrorCode.NeedsTwoFactorAuthenticationCode, this);
            }

            return Task.FromResult(lastTwoFactorCodeProvided);
        }

        public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        {
            if (previousCodeWasIncorrect || string.IsNullOrWhiteSpace(lastEmailCodeProvided))
            {
                this.lastEmailCodeProvided = "";
                throw new UserLoginException(UserLoginErrorCode.NeedsEmailVerificationCode, this);
            }

            return Task.FromResult(lastEmailCodeProvided);
        }

        public Task<bool> AcceptDeviceConfirmationAsync()
        {
            return Task.FromResult(false);
        }
    }
}