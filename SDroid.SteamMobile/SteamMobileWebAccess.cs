using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SDroid.SteamWeb;

namespace SDroid.SteamMobile
{
    public class SteamMobileWebAccess : SteamWebAccess
    {
        protected const string ClientUserAgent =
            "Mozilla/5.0 (Linux; Android 6.0; Nexus 6P Build/XXXXX; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/47.0.2526.68 Mobile Safari/537.36";

        protected const string ClientXRequestedWith = "com.valvesoftware.android.steam.community";

        public SteamMobileWebAccess(MobileSession session) : base(session)
        {
        }

        public SteamMobileWebAccess(MobileSession session, IPAddress ipAddress) : base(session, ipAddress)
        {
        }

        public SteamMobileWebAccess(MobileSession session, IWebProxy proxy) : base(session, proxy)
        {
        }
        public SteamMobileWebAccess(MobileSession session, IPAddress ipAddress, IWebProxy proxy) : base(session, ipAddress, proxy)
        {
        }

        public new static SteamMobileWebAccess GetGuest()
        {
            return new SteamMobileWebAccess(new MobileSession());
        }

        public new static SteamMobileWebAccess GetGuest(IPAddress ipAddress)
        {
            return new SteamMobileWebAccess(new MobileSession(), ipAddress);
        }

        public new static SteamMobileWebAccess GetGuest(IWebProxy proxy)
        {
            return new SteamMobileWebAccess(new MobileSession(), proxy);
        }

        /// <inheritdoc />
        protected override async Task<HttpWebRequest> MakeRequest(
            string url,
            byte[] body,
            SteamWebAccessRequest accessRequest
        )
        {
            var webRequest = await base.MakeRequest(url, body, accessRequest).ConfigureAwait(false);
            webRequest.UserAgent = ClientUserAgent;

            if (!webRequest.Headers.AllKeys.Contains("X-Requested-With"))
            {
                webRequest.Headers.Add("X-Requested-With", ClientXRequestedWith);
            }

            return webRequest;
        }
    }
}