using Newtonsoft.Json;

namespace SDroid.SteamMobile.InternalModels
{
    internal class BeginAuthSessionViaCredentialsConfirmation
    {
        [JsonProperty("confirmation_type")]
        public AuthConfirmationType ConfirmationType { get; set; }

    }
}
