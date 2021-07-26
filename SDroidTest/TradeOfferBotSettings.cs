using SDroid.Interfaces;
using SDroid.SteamTrade.Models.TradeOffer;
using SDroid.SteamWeb;

namespace SDroidTest
{
    // ReSharper disable once InconsistentNaming
    public class TradeOfferBotSettings : ISampleBotSettings, ITradeOfferBotSettings
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
        public TradeOfferOptions TradeOfferOptions { get; set; }
    }
}