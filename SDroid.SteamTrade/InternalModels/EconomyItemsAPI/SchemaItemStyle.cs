using System.Collections.Generic;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaItemStyle
    {
        [JsonProperty("additional_hidden_bodygroups")]
        public Dictionary<string, bool> AdditionalHiddenBodyGroups { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}