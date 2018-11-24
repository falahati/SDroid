using Newtonsoft.Json;

namespace SDroid.SteamWeb.InternalModels
{
    internal class LoginResponse
    {
        [JsonProperty("captcha_gid")]
        public string CaptchaGID { get; set; }

        [JsonProperty("captcha_needed")]
        public bool CaptchaNeeded { get; set; }

        [JsonProperty("emailauth_needed")]
        public bool EmailAuthNeeded { get; set; }

        [JsonProperty("emaildomain")]
        public string EmailDomain { get; set; }

        [JsonProperty("emailsteamid")]
        public ulong EmailSteamId { get; set; }

        [JsonProperty("login_complete")]
        public bool LoginComplete { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("requires_twofactor")]
        public bool TwoFactorNeeded { get; set; }
    }
}