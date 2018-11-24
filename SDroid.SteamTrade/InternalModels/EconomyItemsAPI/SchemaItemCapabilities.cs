using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaItemCapabilities
    {
        [JsonProperty("can_be_restored")]
        public bool CanBeRestored { get; set; }

        [JsonProperty("can_card_upgrade")]
        public bool CanCardUpgrade { get; set; }

        [JsonProperty("can_consume")]
        public bool CanConsume { get; set; }

        [JsonProperty("can_craft_count")]
        public bool CanCraftCount { get; set; }

        [JsonProperty("can_craft_mark")]
        public bool CanCraftMark { get; set; }

        [JsonProperty("can_gift_wrap")]
        public bool CanGiftWrap { get; set; }

        [JsonProperty("can_killstreakify")]
        public bool CanKillStreakify { get; set; }

        [JsonProperty("can_strangify")]
        public bool CanStrangify { get; set; }

        [JsonProperty("nameable")]
        public bool IsNameable { get; set; }

        [JsonProperty("paintable")]
        public bool IsPaintable { get; set; }

        [JsonProperty("strange_parts")]
        public bool IsStrangeParts { get; set; }

        [JsonProperty("usable")]
        public bool IsUsable { get; set; }

        [JsonProperty("usable_gc")]
        public bool IsUsableGc { get; set; }

        [JsonProperty("usable_out_of_game")]
        public bool IsUsableOutOfGame { get; set; }
    }
}