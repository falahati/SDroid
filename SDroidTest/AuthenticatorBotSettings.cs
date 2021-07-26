using SDroid.Interfaces;
using SDroid.SteamMobile;
using SDroid.SteamWeb;

namespace SDroidTest
{
    internal class AuthenticatorBotSettings : ISampleBotSettings, IAuthenticatorSettings
    {
        /// <inheritdoc />
        public Authenticator Authenticator { get; set; }

        /// <inheritdoc />
        public int ConfirmationCheckInterval { get; set; } = 60;

        /// <inheritdoc />
        public string ApiKey { get; set; }

        /// <inheritdoc />
        public string DomainName { get; set; }

        /// <inheritdoc />
        public string Proxy { get; set; }

        /// <inheritdoc />
        public string PublicIPAddress { get; set; }

        /// <inheritdoc />
        public WebSession Session { get; set; }

        /// <inheritdoc />
        public int SessionCheckInterval { get; set; } = 60;

        /// <inheritdoc cref="ISampleBotSettings" />
        public string Username { get; set; }

        /// <inheritdoc />
        public void SaveSettings()
        {
            this.Save();
        }
    }
}