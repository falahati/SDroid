using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.TradeJson
{
    internal class TradeUserCurrency
    {
        public TradeUserCurrency(Currency currency)
        {
            AppId = currency.AppId;
            ContextId = currency.ContextId;
            CurrencyId = currency.CurrencyId;
            Amount = currency.Amount;
        }

        public TradeUserCurrency()
        {
        }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("appid")]
        public long AppId { get; set; }

        [JsonProperty("contextid")]
        public long ContextId { get; set; }

        [JsonProperty("currencyid")]
        public long CurrencyId { get; set; }

        public Currency ToCurrency()
        {
            return new Currency(AppId, ContextId, CurrencyId, Amount);
        }
    }
}