using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class ItemEquippedSlot
    {
        [JsonProperty("class")]
        public int CharacterClassId { get; set; }

        [JsonProperty("slot")]
        public int CharacterSlotId { get; set; }
    }
}