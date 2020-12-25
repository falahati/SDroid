using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyServiceAPI
{
    internal class GetTradeHoldDurationsResponse
    {
        [JsonProperty("my_escrow")]
        public TradeHoldDuration MyEscrow { get; set; }

        [JsonProperty("their_escrow")]
        public TradeHoldDuration TheirEscrow { get; set; }

        [JsonProperty("both_escrow")]
        public TradeHoldDuration BothEscrow { get; set; }
    }
}