using System.Net;
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
        public string Password { get; set; }

        /// <inheritdoc />
        public IWebProxy Proxy { get; set; } = null;

        /// <inheritdoc />
        public IPAddress PublicIPAddress { get; set; } = IPAddress.Any;

        /// <inheritdoc />
        public WebSession Session { get; set; }

        /// <inheritdoc />
        public int SessionCheckInterval { get; set; } = 10;

        /// <inheritdoc />
        public string Username { get; set; }

        /// <inheritdoc />
        public void SaveSettings()
        {
        }

        /// <inheritdoc />
        public TradeOfferOptions TradeOfferOptions { get; set; }
    }
}