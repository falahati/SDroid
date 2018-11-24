using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class DeclineTradeOfferResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}