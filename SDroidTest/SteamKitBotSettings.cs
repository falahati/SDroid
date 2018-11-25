using System.IO;
using ConsoleUtilities;
using Newtonsoft.Json;
using SDroid.Interfaces;
using SDroid.SteamWeb;

namespace SDroidTest
{
    class SteamKitBotSettings : ISteamKitBotSettings
    {
        /// <inheritdoc />
        public void SaveSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText("SteamKitBotSettings.json", json);
            }
            catch
            {
                // ignored
            }
        }

        public static SteamKitBotSettings LoadSaved()
        {
            if (File.Exists("SteamKitBotSettings.json"))
            {
                try
                {
                    var json = File.ReadAllText("SteamKitBotSettings.json");

                    var retVal = JsonConvert.DeserializeObject<SteamKitBotSettings>(json);

                    if (retVal != null)
                    {
                        return retVal;
                    }
                }
                catch
                {
                    // ignored
                }
            }
            return new SteamKitBotSettings
            {
                Username = ConsoleWriter.Default.PrintQuestion("Username")
            };
        }

        /// <inheritdoc />
        public string ApiKey { get; set; }

        /// <inheritdoc />
        public string DomainName { get; set; }

        /// <inheritdoc />
        public string Proxy { get; set; } = null;

        /// <inheritdoc />
        public string PublicIPAddress { get; set; } = "0.0.0.0";

        /// <inheritdoc />
        public WebSession Session { get; set; }

        /// <inheritdoc />
        public int SessionCheckInterval { get; set; } = 60;

        /// <inheritdoc />
        public string Username { get; set; }

        /// <inheritdoc />
        public int LoginTimeout { get; set; } = 300;

        /// <inheritdoc />
        public byte[] SentryFileHash { get; set; }

        /// <inheritdoc />
        public byte[] SentryFile { get; set; }

        /// <inheritdoc />
        public string SentryFileName { get; set; }

        /// <inheritdoc />
        public string LoginKey { get; set; }
    }
}
