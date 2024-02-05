using System.Collections.Generic;
using Newtonsoft.Json;

namespace SDroid.SteamMobile.InternalModels
{
    internal class FinalizeLoginResponse
    {
        [JsonProperty("steamID")]
        public ulong SteamId { get; set; }

        [JsonProperty("redir")]
        public string Redirect { get; set; }

        [JsonProperty("transfer_info")]
        public List<FinalizeLoginTransferInfo> TransferInformation { get; set; }

        [JsonProperty("primary_domain")]
        public string PrimaryDomain { get; set; }
    }
}