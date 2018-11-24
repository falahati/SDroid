using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.MobileConfigurationsJson
{
    internal class SendConfirmationResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}