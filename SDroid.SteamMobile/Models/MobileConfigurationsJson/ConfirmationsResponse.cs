using System.Collections.Generic;
using Newtonsoft.Json;

namespace SDroid.SteamMobile.Models.MobileConfigurationsJson
{
    internal class ConfirmationsResponse
    {

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("needauth")]
        public bool NeedsAuthentication { get; set; }

        [JsonProperty("conf")]
        public List<ConfirmationResponse> Confirmations { get; set; }
    }
}