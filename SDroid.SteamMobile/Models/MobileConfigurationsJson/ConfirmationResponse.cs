using Newtonsoft.Json;
using System.Collections.Generic;

namespace SDroid.SteamMobile.Models.MobileConfigurationsJson
{
    internal class ConfirmationResponse
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }

        [JsonProperty("nonce")]
        public ulong Nonce { get; set; }

        [JsonProperty("type")]
        public ConfirmationType Type { get; set; }

        [JsonProperty("type_name")]
        public string TypeName { get; set; }

        [JsonProperty("cancel")]
        public string CancelTitle { get; set; }

        [JsonProperty("accept")]
        public string AcceptTitle { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("multi")]
        public bool IsMulti { get; set; }

        [JsonProperty("headline")]
        public string Headline { get; set; }

        [JsonProperty("summary")]
        public List<string> Summary { get; set; }

        [JsonProperty("warn")]
        public List<string> Warning { get; set; }

        [JsonProperty("creator_id")]
        public ulong CreatorId { get; set; }

        [JsonProperty("creation_time")]
        public uint CreationTime { get; set; }
    }
}