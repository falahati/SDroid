using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class TradeHoldDuration
    {
        [JsonProperty("escrow_end_duration_seconds")]
        public int HoldDurationInSeconds { get; set; }
    }
}