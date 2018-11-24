using Newtonsoft.Json;
using SDroid.SteamTrade.InternalModels.EconomyItemsAPI.Constants;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class GetPlayerItemsResult
    {
        [JsonProperty("num_backpack_slots")]
        public int BackpackSlots { get; set; }

        [JsonProperty("items")]
        public Item[] Items { get; set; }

        [JsonProperty("status")]
        public GetPlayerItemsStatus Status { get; set; }
    }
}