using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class CancelTradeOfferResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}