using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.SteamGuardPhoneOperationJson
{
    internal class HasPhoneResponse
    {
        [JsonProperty("has_phone")]
        public bool HasPhone { get; set; }
    }
}