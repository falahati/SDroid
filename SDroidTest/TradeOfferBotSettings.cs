using System.IO;
using ConsoleUtilities;
using Newtonsoft.Json;
using SDroid.Interfaces;
using SDroid.SteamTrade.Models.TradeOffer;
using SDroid.SteamWeb;

namespace SDroidTest
{
    public class TradeOfferBotSettings : IBotSettings, ITradeOfferBotSettings
    {
        /// <inheritdoc />
        public string ApiKey { get; set; }

        /// <inheritdoc />
        public string DomainName { get; set; }

        /// <inheritdoc />
        public string Proxy { get; set; }

        /// <inheritdoc />
        public string PublicIPAddress { get; set; } = "0.0.0.0";

        /// <inheritdoc />
        public WebSession Session { get; set; }

        /// <inheritdoc />
        public int SessionCheckInterval { get; set; } = 60;

        /// <inheritdoc />
        public string Username { get; set; }

        /// <inheritdoc />
        public void SaveSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText("TradeOfferBotSettings.json", json);
            }
            catch
            {
                // ignored
            }
        }

        /// <inheritdoc />
        public TradeOfferOptions TradeOfferOptions { get; set; }

        public static TradeOfferBotSettings LoadSaved()
        {
            if (File.Exists("TradeOfferBotSettings.json"))
            {
                try
                {
                    var json = File.ReadAllText("TradeOfferBotSettings.json");
                    var retVal = JsonConvert.DeserializeObject<TradeOfferBotSettings>(json);

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

            return new TradeOfferBotSettings
            {
                Username = ConsoleWriter.Default.PrintQuestion("Username")
            };
        }
    }
}