using Newtonsoft.Json;

namespace SDroid.SteamMobile.InternalModels
{
    internal class GetPasswordRsaPublicKeyResponse
    {
        [JsonProperty("publickey_exp")]
        public string Exponent { get; set; }

        [JsonProperty("publickey_mod")]
        public string Modulus { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
    }
}