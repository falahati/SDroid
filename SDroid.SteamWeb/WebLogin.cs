using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamWeb.Exceptions;
using SDroid.SteamWeb.InternalModels;

namespace SDroid.SteamWeb
{
    /// <summary>
    ///     Handles logging the user into the mobile Steam website. Necessary to generate OAuth token and session cookies.
    /// </summary>
    public class WebLogin
    {
        protected const string LoginCaptchaUrl = SteamWebAccess.CommunityBaseUrl + "/login/rendercaptcha/";

        protected const string LoginInitializeUrl = SteamWebAccess.CommunityBaseUrl + "/login";

        protected const string WebLoginRSAUrl = SteamWebAccess.CommunityBaseUrl + "/login/getrsakey";

        protected const string WebLoginUrl = SteamWebAccess.CommunityBaseUrl + "/login/dologin";

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);
        protected byte[] CachedCaptchaImage { get; set; }

        /// <summary>
        ///     Gets the captcha GID required to get the captcha image associated with the latest login attempt.
        /// </summary>
        /// <value>
        ///     The captcha gid identification string
        /// </value>
        public string CaptchaGID { get; protected set; }

        /// <summary>
        ///     Gets the domain name of the email address associated with this account
        /// </summary>
        /// <value>
        ///     The email address domain name
        /// </value>
        public string EmailDomain { get; protected set; }

        protected SemaphoreSlim LockObject { get; } = new SemaphoreSlim(1, 1);

        public bool RequiresCaptchaCode { get; protected set; }

        /// <summary>
        ///     Gets a value indicating whether email verification is required.
        /// </summary>
        /// <value>
        ///     <c>true</c> if user needs to verify his/her access to the email address associated with this user account;
        ///     otherwise, <c>false</c>.
        /// </value>
        public bool RequiresEmailVerification { get; protected set; }

        /// <summary>
        ///     Gets a value indicating whether two factor authentication code is required.
        /// </summary>
        /// <value>
        ///     <c>true</c> if user needs to verify his identity by providing the two factor authentication code generated for this
        ///     user account; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresTwoFactorAuthenticationCode { get; protected set; }

        /// <summary>
        ///     Gets the Steam account identifier provided for recognizing user's account in case of email and/or two factor
        ///     authentication requirement
        /// </summary>
        /// <value>
        ///     The Steam account identifier
        /// </value>
        public ulong? SteamId { get; protected set; }

        protected SteamWebAccess SteamWebAccess { get; set; }

        protected static byte[] HexStringToByteArray(string hex)
        {
            var hexLen = hex.Length;
            var ret = new byte[hexLen / 2];

            for (var i = 0; i < hexLen; i += 2)
            {
                ret[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return ret;
        }

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
        public virtual async Task<WebSession> DoLogin(LoginCredentials credentials)
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

                var loginRequest = await ConstructLoginRequest(credentials).ConfigureAwait(false);

                var loginResponse = loginRequest != null
                    ? await OperationRetryHelper.Default
                        .RetryOperationAsync(() => SteamWebAccess.FetchString(loginRequest)).ConfigureAwait(false)
                    : null;

                if (loginResponse == null)
                {
                    throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
                }

                if (!await ProcessLoginResponse(loginResponse).ConfigureAwait(false))
                {
                    throw new UserLoginException(UserLoginErrorCode.BadCredentials, this);
                }

                var sessionData = SteamWebAccess.Session;
                ResetStates();

                return sessionData;
            }
            finally
            {
                // Unlock this instance
                LockObject.Release();
            }
        }


        /// <summary>
        ///     Downloads the captcha image associated with the latest login attempt.
        /// </summary>
        /// <returns>An array of bytes representing an image in PNG file format</returns>
        public virtual async Task<byte[]> DownloadCaptchaImage()
        {
            if (CachedCaptchaImage != null)
            {
                return CachedCaptchaImage;
            }

            if (string.IsNullOrWhiteSpace(CaptchaGID))
            {
                return null;
            }

            // Lock this instance
            await LockObject.WaitAsync().ConfigureAwait(false);
            CachedCaptchaImage = null;

            try
            {
                CachedCaptchaImage = (await OperationRetryHelper.Default.RetryOperationAsync(
                    () => SteamWebAccess.FetchBinary(
                        new SteamWebAccessRequest(
                            LoginCaptchaUrl,
                            SteamWebAccessRequestMethod.Get,
                            new QueryStringBuilder
                            {
                                {"gid", CaptchaGID}
                            }
                        ))
                ).ConfigureAwait(false)).ToArray();

                return CachedCaptchaImage;
            }
            finally
            {
                // Unlock this instance
                LockObject.Release();
            }
        }

        /// <summary>
        ///     Resets this instance state and allows for a new authentication process to start.
        /// </summary>
        /// <returns><c>true</c> if a new guest session identification string retrieved; otherwise, <c>false</c>.</returns>
        public virtual async Task<bool> Reset()
        {
            // Lock this instance
            await LockObject.WaitAsync().ConfigureAwait(false);

            try
            {
                ResetStates();

                return await GetGuestSession().ConfigureAwait(false);
            }
            finally
            {
                // Unlock this instance
                LockObject.Release();
            }
        }

        protected virtual async Task<SteamWebAccessRequest> ConstructLoginRequest(LoginCredentials credentials)
        {
            // Get a RSA public key for password encryption
            var serverResponse = await SteamWebAccess.FetchString(
                new SteamWebAccessRequest(
                    WebLoginRSAUrl,
                    SteamWebAccessRequestMethod.Post,
                    new QueryStringBuilder
                    {
                        {"donotcache", (DateTime.UtcNow - Epoch).TotalMilliseconds},
                        {"username", credentials.UserName}
                    }
                )
            ).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(serverResponse) ||
                serverResponse.Contains("<BODY>\nAn error occurred while processing your request."))
            {
                throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
            }

            var rsaResponse = JsonConvert.DeserializeObject<RSAResponse>(serverResponse);

            if (rsaResponse?.Success != true)
            {
                throw new UserLoginException(UserLoginErrorCode.BadRSAResponse, this);
            }

            // Sleep for a bit to give Steam a chance to catch up??
            await Task.Delay(350).ConfigureAwait(false);

            string encryptedPassword;

            using (var rsaEncrypt = new RSACryptoServiceProvider())
            {
                rsaEncrypt.ImportParameters(new RSAParameters
                {
                    Exponent = HexStringToByteArray(rsaResponse.Exponent),
                    Modulus = HexStringToByteArray(rsaResponse.Modulus)
                });

                encryptedPassword =
                    Convert.ToBase64String(
                        rsaEncrypt.Encrypt(
                            Encoding.UTF8.GetBytes(credentials.Password)
                            , false)
                    );
            }

            return new SteamWebAccessRequest(
                WebLoginUrl,
                SteamWebAccessRequestMethod.Post,
                new QueryStringBuilder
                {
                    {"donotcache", (DateTime.UtcNow - Epoch).TotalMilliseconds},
                    {"rsatimestamp", rsaResponse.Timestamp},
                    {"password", encryptedPassword},
                    {"username", credentials.UserName},
                    {"twofactorcode", credentials.TwoFactorAuthenticationCode ?? ""},
                    {"emailauth", RequiresEmailVerification ? (credentials.EmailVerificationCode ?? "") : ""},
                    {"loginfriendlyname", ""},
                    {"captchagid", RequiresCaptchaCode ? (CaptchaGID ?? "-1") : "-1"},
                    {"captcha_text", RequiresCaptchaCode ? (credentials.CaptchaCode ?? "") : ""},
                    {
                        "emailsteamid",
                        RequiresTwoFactorAuthenticationCode || RequiresEmailVerification
                            ? (SteamId?.ToString() ?? "")
                            : ""
                    },
                    {"rsatimestamp", rsaResponse.Timestamp},
                    {"remember_login", "true"}
                }
            );
        }

        protected virtual async Task<bool> GetGuestSession()
        {
            // Get a new SessionId
            SteamWebAccess = SteamWebAccess.GetGuest();
            (await OperationRetryHelper.Default
                .RetryOperationAsync(() => SteamWebAccess.FetchBinary(new SteamWebAccessRequest(LoginInitializeUrl)))
                .ConfigureAwait(false)).Dispose();

            return !string.IsNullOrWhiteSpace(SteamWebAccess?.Session?.SessionId);
        }

        protected virtual Task<bool> ProcessLoginResponse(string response)
        {
            var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(response);

            if (loginResponse == null)
            {
                throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
            }

            if (loginResponse.Message?.ToLower().Contains("incorrect login") == true ||
                loginResponse.Message?.ToLower().Contains("password") == true &&
                loginResponse.Message?.ToLower().Contains("incorrect") == true)
            {
                throw new UserLoginException(UserLoginErrorCode.BadCredentials, this);
            }

            if (loginResponse.CaptchaNeeded)
            {
                RequiresCaptchaCode = true;
                CaptchaGID = loginResponse.CaptchaGID;
                CachedCaptchaImage = null;

                throw new UserLoginException(UserLoginErrorCode.NeedsCaptchaCode, this);
            }

            if (!string.IsNullOrWhiteSpace(loginResponse.CaptchaGID) &&
                loginResponse.CaptchaGID != "-1" &&
                CaptchaGID != loginResponse.CaptchaGID)
            {
                CaptchaGID = loginResponse.CaptchaGID;
                CachedCaptchaImage = null;
            }

            if (loginResponse.EmailAuthNeeded)
            {
                RequiresEmailVerification = true;
                SteamId = loginResponse.EmailSteamId > 0 ? loginResponse.EmailSteamId : SteamId;
                EmailDomain = loginResponse.EmailDomain;

                throw new UserLoginException(UserLoginErrorCode.NeedsEmailVerificationCode, this);
            }

            if (loginResponse.TwoFactorNeeded && !loginResponse.Success)
            {
                RequiresTwoFactorAuthenticationCode = true;
                SteamId = loginResponse.EmailSteamId > 0 ? loginResponse.EmailSteamId : SteamId;

                throw new UserLoginException(UserLoginErrorCode.NeedsTwoFactorAuthenticationCode, this);
            }

            if (loginResponse.EmailSteamId > 0 && SteamId != loginResponse.EmailSteamId)
            {
                SteamId = loginResponse.EmailSteamId;
            }

            if (loginResponse.Message?.Contains("too many login failures") == true)
            {
                throw new UserLoginException(UserLoginErrorCode.TooManyFailedLoginAttempts, this);
            }

            return Task.FromResult(loginResponse.LoginComplete);
        }


        protected virtual void ResetStates()
        {
            // Reset variables
            SteamWebAccess = null;
            RequiresCaptchaCode = false;
            RequiresTwoFactorAuthenticationCode = false;
            RequiresEmailVerification = false;
            CaptchaGID = null;
            CachedCaptchaImage = null;
            SteamId = null;
        }
    }
}