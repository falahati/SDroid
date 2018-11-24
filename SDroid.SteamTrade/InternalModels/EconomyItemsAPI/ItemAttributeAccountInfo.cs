using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class ItemAttributeAccountInfo
    {
        [JsonProperty("steamid")]
        public ulong CommunitySteamId { get; set; }

        [JsonProperty("personaname")]
        public string PersonaName { get; set; }
    }
}