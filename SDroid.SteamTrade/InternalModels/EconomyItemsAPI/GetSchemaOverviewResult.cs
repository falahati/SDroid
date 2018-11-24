using System.Collections.Generic;
using Newtonsoft.Json;
using SDroid.SteamTrade.InternalModels.EconomyItemsAPI.Constants;

namespace SDroid.SteamTrade.InternalModels.EconomyItemsAPI
{
    internal class GetSchemaOverviewResult
    {
        [JsonProperty("attribute_controlled_attached_particles")]
        public SchemaOverviewAttributeControlledParticle[] AttributeControlledAttachedParticles { get; set; }

        [JsonProperty("attributes")]
        public SchemaOverviewAttribute[] Attributes { get; set; }

        [JsonProperty("item_levels")]
        public SchemaOverviewItemLevel[] ItemLevels { get; set; }

        [JsonProperty("item_sets")]
        public SchemaOverviewItemSet[] ItemSets { get; set; }

        [JsonProperty("items_game_url")]
        public string ItemsGameUrl { get; set; }

        [JsonProperty("kill_eater_score_types")]
        public SchemaOverviewKillEaterScoreType[] KillEaterScoreTypes { get; set; }

        [JsonProperty("originNames")]
        public SchemaOverviewOriginName[] OriginNames { get; set; }

        [JsonProperty("qualities")]
        public Dictionary<string, int> Qualities { get; set; }

        [JsonProperty("qualityNames")]
        public Dictionary<string, string> QualityNames { get; set; }

        [JsonProperty("status")]
        public GetSchemaOverviewStatus Status { get; set; }

        [JsonProperty("string_lookups")]
        public SchemaOverviewStringLockup[] StringLookups { get; set; }
    }
}