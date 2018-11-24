using System;

namespace SDroid.SteamWeb.Exceptions
{
    /// <summary>
    ///     Represents an error that happened during the login process started with an instance of UserLogin class
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class UserLoginException : Exception
    {
        public UserLoginException(
            UserLoginErrorCode errorCode,
            WebLogin loginInstance) : base(GetMessage(errorCode))
        {
            ErrorCode = errorCode;
            UserLogin = loginInstance;
        }

        /// <summary>
        ///     Gets the error code of this exception
        /// </summary>
        /// <value>
        ///     The error code
        /// </value>
        public UserLoginErrorCode ErrorCode { get; }

        /// <summary>
        ///     Gets the instance of UserLogin that raised this exception
        /// </summary>
        /// <value>
        ///     The UserLogin instance
        /// </value>
        public WebLogin UserLogin { get; }

        private static string GetMessage(UserLoginErrorCode? errorCode)
        {
            switch (errorCode)
            {
                case UserLoginErrorCode.BadRSAResponse:

                    return "Server responded with an invalid RSA public key.";
                case UserLoginErrorCode.BadCredentials:

                    return "User credentials are missing or invalid.";
                case UserLoginErrorCode.NeedsCaptchaCode:

                    return "Captcha verification is necessary.";
                case UserLoginErrorCode.NeedsTwoFactorAuthenticationCode:

                    return "Two factor authentication code is necessary.";
                case UserLoginErrorCode.NeedsEmailVerificationCode:

                    return "Email address verification is necessary.";
                case UserLoginErrorCode.TooManyFailedLoginAttempts:

                    return "Too many failed attempts to login received in the past.";
                default:

                    return "Login process failed due to an general failure or a bad response.";
            }
        }
    }
}