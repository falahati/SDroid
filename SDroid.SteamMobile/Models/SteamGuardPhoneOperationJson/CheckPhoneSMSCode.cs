using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.SteamGuardPhoneOperationJson
{
    internal class CheckPhoneSMSCode
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}