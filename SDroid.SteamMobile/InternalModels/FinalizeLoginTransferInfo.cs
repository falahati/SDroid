using Newtonsoft.Json;

namespace SDroid.SteamMobile.InternalModels
{
    internal class FinalizeLoginTransferInfo
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("params")]
        public FinalizeLoginTransferParameters Parameters { get; set; }
    }
}