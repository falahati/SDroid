using Newtonsoft.Json;
using SDroid.SteamTrade.Models.Trade;

namespace SDroid.SteamTrade.InternalModels.TradeJson
{
    internal class TradeState
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("events")]
        public TradeEvent[] Events { get; set; }

        [JsonProperty("logpos")]
        public int LogPosition { get; set; }

        [JsonProperty("me")]
        public TradeUserObject Me { get; set; }

        [JsonProperty("newversion")]
        public bool NewVersion { get; set; }

        [JsonProperty("trade_status")]
        public TradeStateStatus Status { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("them")]
        public TradeUserObject Them { get; set; }

        [JsonProperty("tradeid")]
        public long TradeId { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }
    }
}