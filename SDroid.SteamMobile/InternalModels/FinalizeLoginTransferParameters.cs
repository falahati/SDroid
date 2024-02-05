using Newtonsoft.Json;

namespace SDroid.SteamMobile.InternalModels
{
    internal class FinalizeLoginTransferParameters
    {
        [JsonProperty("nonce")]
        public string Nonce { get; set; }

        [JsonProperty("auth")]
        public string Auth { get; set; }
    }
}