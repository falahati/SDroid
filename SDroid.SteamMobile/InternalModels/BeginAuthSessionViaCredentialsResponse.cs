using System.Collections.Generic;
using Newtonsoft.Json;

namespace SDroid.SteamMobile.InternalModels
{
    internal class BeginAuthSessionViaCredentialsResponse
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("request_id")]
        public string RequestId { get; set; }

        [JsonProperty("steamid")]
        public ulong SteamId { get; set; }

        [JsonProperty("allowed_confirmations")]
        public List<BeginAuthSessionViaCredentialsConfirmation> AllowedConfirmations { get; set; }

        [JsonProperty("weak_token")]
        public string WeakToken { get; set; }

        [JsonProperty("extended_error_message")]
        public string ExtendedErrorMessage { get; set; }
    }
}