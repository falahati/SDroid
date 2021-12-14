using SDroid.Interfaces;
using SDroid.SteamWeb;

namespace SDroidTest
{
    internal class SteamKitBotSettings : ISteamKitBotSettings, ISampleBotSettings
    {
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

        /// <inheritdoc />
        public string LoginKey { get; set; }

        /// <inheritdoc />
        public int LoginTimeout { get; set; } = 300;

        /// <inheritdoc />
        public byte[] SentryFile { get; set; }

        /// <inheritdoc />
        public byte[] SentryFileHash { get; set; }

        /// <inheritdoc />
        public string SentryFileName { get; set; }

        public int? ConnectionTimeout { get; set; }
    }
}