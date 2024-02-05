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

namespace SDroid.SteamMobile
{
    /// <summary>
    ///     Handles logging the user into the mobile Steam website. Necessary to generate OAuth token and session cookies.
    /// </summary>
    public class MobileLogin : WebLogin
    {
        private const string LoginSteamPoweredBaseUrl = "https://login.steampowered.com";
        private const string ClientOAuthId = "DE45CD61";
        private const string ClientOAuthScope = "read_profile write_profile read_client write_client";
        private string FinalizeLoginUrl => $"{LoginSteamPoweredBaseUrl}/jwt/finalizelogin";


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

                var apiAccess = new SteamWebAPI(SteamWebAccess);

                // Get a RSA public key for password encryption
                var rsaResponse = await apiAccess.RequestObject<SteamWebAPIResponse<GetPasswordRsaPublicKeyResponse>>(
                    "IAuthenticationService",
                    SteamWebAccessRequestMethod.Get,
                    "GetPasswordRSAPublicKey",
                    "v1",
                    new
                    {
                        account_name = credentials.UserName
                    }
                ).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(rsaResponse?.Response.Exponent))
                {
                    throw new UserLoginException(UserLoginErrorCode.BadRSAResponse, this);
                }

                // Sleep for a bit to give Steam a chance to catch up??
                await Task.Delay(350).ConfigureAwait(false);

                string password;
                using (var rsaEncrypt = new RSACryptoServiceProvider())
                {
                    rsaEncrypt.ImportParameters(new RSAParameters
                    {
                        Exponent = HexStringToByteArray(rsaResponse.Response.Exponent),
                        Modulus = HexStringToByteArray(rsaResponse.Response.Modulus)
                    });

                    password = Convert.ToBase64String(
                        rsaEncrypt.Encrypt(
                            Encoding.UTF8.GetBytes(credentials.Password)
                            , false)
                    );
                }

                var beginAuthResponse = await apiAccess
                    .RequestObject<SteamWebAPIResponse<BeginAuthSessionViaCredentialsResponse>>(
                        "IAuthenticationService",
                        SteamWebAccessRequestMethod.Post,
                        "BeginAuthSessionViaCredentials",
                        "v1",
                        new
                        {
                            persistence = 1,
                            encrypted_password = password,
                            account_name = credentials.UserName,
                            encryption_timestamp = rsaResponse.Response.Timestamp,
                            device_details = new {
                                platform_type = 3,
                                os_type = -496,
                            }
                        }
                    ).ConfigureAwait(false);

                if (string.IsNullOrEmpty(beginAuthResponse?.Response?.ClientId))
                {
                    throw new UserLoginException(UserLoginErrorCode.BadCredentials, this);
                }

                if (
                    beginAuthResponse.Response.AllowedConfirmations.Any(
                        c => c.ConfirmationType == AuthConfirmationType.EmailCode
                    ) &&
                    beginAuthResponse.Response.AllowedConfirmations.All(
                        c => c.ConfirmationType != AuthConfirmationType.TwoFactorCode
                    )
                )
                {
                    // if only email confirmation is accepted

                    if (!string.IsNullOrWhiteSpace(credentials.EmailVerificationCode))
                    {
                        var updateAuthResponse = await apiAccess.RequestObject<SteamWebAPIResponse<dynamic>>(
                            "IAuthenticationService",
                            SteamWebAccessRequestMethod.Post,
                            "UpdateAuthSessionWithSteamGuardCode",
                            "v1",
                            new
                            {
                                client_id = beginAuthResponse.Response.ClientId,
                                steamid = beginAuthResponse.Response.SteamId,
                                code_type = AuthConfirmationType.EmailCode,
                                code = credentials.EmailVerificationCode
                            }
                        ).ConfigureAwait(false);

                        if (updateAuthResponse?.Response == null)
                        {
                            RequiresEmailVerification = true;
                            throw new UserLoginException(UserLoginErrorCode.NeedsEmailVerificationCode, this);
                        }
                    }
                    else
                    {
                        RequiresEmailVerification = true;
                        throw new UserLoginException(UserLoginErrorCode.NeedsEmailVerificationCode, this);
                    }
                }
                else if (
                    beginAuthResponse.Response.AllowedConfirmations.Any(
                        c => c.ConfirmationType == AuthConfirmationType.TwoFactorCode
                    )
                )
                {
                    // if two factor confirmation is accepted
                    if (!string.IsNullOrWhiteSpace(credentials.TwoFactorAuthenticationCode))
                    {
                        var updateAuthResponse = await apiAccess.RequestObject<SteamWebAPIResponse<dynamic>>(
                            "IAuthenticationService",
                            SteamWebAccessRequestMethod.Post,
                            "UpdateAuthSessionWithSteamGuardCode",
                            "v1",
                            new
                            {
                                client_id = beginAuthResponse.Response.ClientId,
                                steamid = beginAuthResponse.Response.SteamId,
                                code_type = AuthConfirmationType.TwoFactorCode,
                                code = credentials.TwoFactorAuthenticationCode
                            }
                        ).ConfigureAwait(false);

                        if (updateAuthResponse?.Response == null)
                        {
                            this.RequiresTwoFactorAuthenticationCode = true;
                            throw new UserLoginException(UserLoginErrorCode.NeedsEmailVerificationCode, this);
                        }
                    }
                    else
                    {
                        this.RequiresTwoFactorAuthenticationCode = true;
                        throw new UserLoginException(UserLoginErrorCode.NeedsTwoFactorAuthenticationCode, this);
                    }
                }
                else if (beginAuthResponse.Response.AllowedConfirmations.Count > 0)
                {
                    throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
                }

                var pollAuthResponse = await apiAccess
                    .RequestObject<SteamWebAPIResponse<PollAuthSessionStatusResponse>>(
                        "IAuthenticationService",
                        SteamWebAccessRequestMethod.Post,
                        "PollAuthSessionStatus",
                        "v1",
                        new
                        {
                            client_id = beginAuthResponse.Response.ClientId,
                            request_id = beginAuthResponse.Response.RequestId
                        }
                    ).ConfigureAwait(false);

                if (
                    string.IsNullOrWhiteSpace(pollAuthResponse?.Response?.AccessToken) ||
                    string.IsNullOrWhiteSpace(pollAuthResponse?.Response?.RefreshToken)
                )
                {
                    throw new UserLoginException(UserLoginErrorCode.BadCredentials, this);
                }

                var finalizeLoginResponse = await OperationRetryHelper.Default.RetryOperationAsync(
                    () => SteamWebAccess.FetchObject<FinalizeLoginResponse>(
                        new SteamWebAccessRequest(
                            FinalizeLoginUrl,
                            SteamWebAccessRequestMethod.Post,
                            QueryStringBuilder.FromDynamic(
                                new
                                {
                                    nonce = pollAuthResponse.Response.RefreshToken,
                                    sessionid = SteamWebAccess.Session.SessionId,
                                    redir = SteamWebAccess.CommunityBaseUrl
                                }
                            )
                        )
                    )
                ).ConfigureAwait(false);

                if (!(finalizeLoginResponse?.TransferInformation?.Count > 0))
                {
                    throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
                }

                foreach (var transferInfo in finalizeLoginResponse.TransferInformation)
                {
                    var response = SteamWebAccess.FetchDynamic(
                        new SteamWebAccessRequest(
                            transferInfo.Url,
                            SteamWebAccessRequestMethod.Post,
                            QueryStringBuilder.FromDynamic(
                                new
                                {
                                    nonce = transferInfo.Parameters.Nonce,
                                    auth = transferInfo.Parameters.Auth,
                                    steamID = finalizeLoginResponse.SteamId
                                }
                            )
                        )
                    );

                    if (response == null)
                    {
                        throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
                    }
                }

                var sessionDate = new MobileSession(SteamWebAccess.Session);
                sessionDate.AccessToken = pollAuthResponse.Response.AccessToken;
                sessionDate.RefreshToken = pollAuthResponse.Response.RefreshToken;

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
    }
}