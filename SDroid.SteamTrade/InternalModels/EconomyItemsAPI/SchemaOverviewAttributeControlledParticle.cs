using Newtonsoft.Json;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class SchemaOverviewAttributeControlledParticle
    {
        [JsonProperty("attachment")]
        public string Attachment { get; set; }

        [JsonProperty("attach_to_rootbone")]
        public bool AttachToRootBone { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("system")]
        public string System { get; set; }
    }
}