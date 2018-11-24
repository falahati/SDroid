using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.SteamGuardPhoneOperationJson
{
    internal class AddPhoneResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}