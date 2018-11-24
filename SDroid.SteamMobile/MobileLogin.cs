using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamMobile.Models.MobileLoginJson;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Exceptions;

namespace SDroid.SteamMobile
{
    /// <summary>
    ///     Handles logging the user into the mobile Steam website. Necessary to generate OAuth token and session cookies.
    /// </summary>
    public class MobileLogin : WebLogin
    {
        protected const string ClientOAuthId = "DE45CD61";
        protected const string ClientOAuthScope = "read_profile write_profile read_client write_client";
        private const string MobileLoginUrl = SteamWebAccess.CommunityBaseUrl + "/mobilelogin";

        /// <inheritdoc />
        protected override async Task<SteamWebAccessRequest> ConstructLoginRequest(LoginCredentials credentials)
        {
            var request = await base.ConstructLoginRequest(credentials).ConfigureAwait(false);

            if (request != null)
            {
                var oAuthArguments = new QueryStringBuilder
                {
                    {"oauth_client_id", ClientOAuthId},
                    {"oauth_scope", ClientOAuthScope}
                };

                request.Data = oAuthArguments.Concat(request.Data);
                request.Referer = oAuthArguments.AppendToUrl(MobileLoginUrl);
            }

            return request;
        }

        protected override async Task<bool> GetGuestSession()
        {
            // Get a new SessionId
            SteamWebAccess = SteamMobileWebAccess.GetGuest();
            (await OperationRetryHelper.Default
                .RetryOperationAsync(() => SteamWebAccess.FetchBinary(new SteamWebAccessRequest(LoginInitializeUrl)))
                .ConfigureAwait(false)).Dispose();

            return !string.IsNullOrWhiteSpace(SteamWebAccess?.Session?.SessionId);
        }

        /// <inheritdoc />
        protected override async Task<bool> ProcessLoginResponse(string response)
        {
            if (await base.ProcessLoginResponse(response).ConfigureAwait(false))
            {
                var loginResponse = JsonConvert.DeserializeObject<MobileLoginResponse>(response);

                if (!(loginResponse.OAuthToken?.OAuthToken?.Length > 0))
                {
                    throw new UserLoginException(UserLoginErrorCode.GeneralFailure, this);
                }

                SteamWebAccess = new SteamMobileWebAccess(new MobileSession(loginResponse.OAuthToken,
                    SteamWebAccess?.Session?.SessionId));
            }

            return false;
        }
    }
}