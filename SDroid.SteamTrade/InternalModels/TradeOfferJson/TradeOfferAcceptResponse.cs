using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.TradeOfferJson
{
    internal class TradeOfferAcceptResponse
    {
        [JsonProperty("strError")]
        public string Error { get; set; }

        [JsonProperty("isaccepted")]
        public bool IsAccepted { get; set; }

        [JsonProperty("needs_email_confirmation")]
        public bool NeedsEmailConfirmation { get; set; }

        [JsonProperty("needs_mobile_confirmation")]
        public bool NeedsMobileConfirmation { get; set; }

        [JsonProperty("tradeid")]
        public long? TradeOfferId { get; set; }
    }
}