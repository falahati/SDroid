using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SDroid.SteamWeb;

namespace SDroid.SteamMobile
{
    public class SteamMobileWebAccess : SteamWebAccess
    {
        protected const string ClientUserAgent =
            "Mozilla/5.0 (Linux; U; Android 4.1.1; en-us; Google Nexus 4 - 4.1.1 - API 16 - 768x1280 Build/JRO03S) AppleWebKit/534.30 (KHTML, like Gecko) Version/4.0 Mobile Safari/534.30";

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
            string postDataString,
            SteamWebAccessRequest accessRequest)
        {
            var webRequest = await base.MakeRequest(url, postDataString, accessRequest).ConfigureAwait(false);
            webRequest.UserAgent = ClientUserAgent;

            if (!webRequest.Headers.AllKeys.Contains("X-Requested-With"))
            {
                webRequest.Headers.Add("X-Requested-With", ClientXRequestedWith);
            }

            return webRequest;
        }
    }
}