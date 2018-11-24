using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.MobileConfigurationsJson
{
    internal class ConfirmationDetailsResponse
    {
        [JsonProperty("html")]
        public string HTML { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}