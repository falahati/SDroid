using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamMobile.Exceptions;
using SDroid.SteamMobile.InternalModels;
using SDroid.SteamMobile.Models.MobileAuthenticationAPI;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Models;

namespace SDroid.SteamMobile
{
    /// <summary>
    ///     Represents pair of OAuth and web session
    /// </summary>
    /// <seealso cref="System.IEquatable{SessionData}" />
    public class MobileSession : WebSession, IEquatable<MobileSession>
    {
        public const string ClientName = "android";
        public const string ClientVersion = "777777 3.6.1";
        private string _refreshToken;
        private string _accessToken;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MobileSession" /> class from a <see cref="WebSession" />.
        /// </summary>
        /// <param name="session">The web session.</param>
        public MobileSession(
            WebSession session
        ) : base(
            session.SteamId,
            session.SteamLoginSecure,
            session.SessionId,
            null
        )
        {
            Cookies.Add(
                new Cookie("mobileClientVersion", ClientVersion, "/", CommunityCookieDomain)
            );

            Cookies.Add(
                new Cookie("mobileClient", ClientName, "/", CommunityCookieDomain)
            );

            SteamId = session.SteamId;
            RefreshToken = null;
            AccessToken = null;

            if (session is MobileSession mobileSession)
            {
                RefreshToken = mobileSession.RefreshToken;
                AccessToken = mobileSession.AccessToken;

                if (string.IsNullOrWhiteSpace(SteamLoginSecure))
                {
                    SteamLoginSecure = $"{SteamId}%7C%7C{AccessToken}";
                }
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MobileSession" /> class.
        /// </summary>
        /// <param name="steamId">The steam user identifier number.</param>
        /// <param name="steamLoginSecure">The steam secure login token.</param>
        /// <param name="sessionId">The session identifier string.</param>
        /// <param name="accessToken">The JWT access token</param>
        /// <param name="refreshToken">The JWT refresh token</param>
        [JsonConstructor]
        public MobileSession(
            ulong steamId,
            string steamLoginSecure,
            string sessionId,
            string accessToken,
            string refreshToken
        ) : base(
            steamId,
            steamLoginSecure,
            sessionId,
            null
        )
        {
            Cookies.Add(
                new Cookie("mobileClientVersion", ClientVersion, "/", CommunityCookieDomain)
            );

            Cookies.Add(
                new Cookie("mobileClient", ClientName, "/", CommunityCookieDomain)
            );

            SteamId = steamId;
            RefreshToken = refreshToken;
            AccessToken = accessToken;

            if (string.IsNullOrWhiteSpace(SteamLoginSecure))
            {
                SteamLoginSecure = $"{SteamId}%7C%7C{AccessToken}";
            }
        }

        public MobileSession() : base()
        {
            Cookies.Add(
                new Cookie("mobileClientVersion", ClientVersion, "/", CommunityCookieDomain)
            );

            Cookies.Add(
                new Cookie("mobileClient", ClientName, "/", CommunityCookieDomain)
            );
        }
        
        /// <summary>
        ///     Gets or sets the JWT refresh token
        /// </summary>
        public string RefreshToken
        {
            get => _refreshToken;
            set => _refreshToken = value;
        }

        /// <summary>
        ///     Gets or sets the JWT access token
        /// </summary>
        public string AccessToken
        {
            get => _accessToken;
            set => _accessToken = value;
        }

        /// <summary>
        ///     Gets the steam user identifier number.
        /// </summary>
        public new ulong SteamId
        {
            get
            {
                var stringValue = Cookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["steamid"]
                    ?.Value;

                if (!string.IsNullOrWhiteSpace(stringValue) && ulong.TryParse(stringValue, out var steamId))
                {
                    return steamId;
                }

                return base.SteamId;
            }
            protected set
            {
                AddCookie("steamid", value.ToString());
                base.SteamId = value;
            }
        }

        /// <inheritdoc />
        public bool Equals(MobileSession other)
        {
            return other != null &&
                   base.Equals(other) &&
                   SteamId == other.SteamId;
        }

        public static bool operator ==(MobileSession data1, MobileSession data2)
        {
            return Equals(data1, data2) || data1?.Equals(data2) == true;
        }

        public static bool operator !=(MobileSession data1, MobileSession data2)
        {
            return !(data1 == data2);
        }

        /// <inheritdoc />
        public override WebSession Clone()
        {
            return new MobileSession(
                SteamId,
                SteamLoginSecure,
                SessionId,
                AccessToken,
                RefreshToken
            );
        }

        /// <inheritdoc />
        public override bool Equals(WebSession other)
        {
            if (other is MobileSession mobileSession)
            {
                return Equals(mobileSession);
            }

            return false;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as MobileSession);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = -823311899;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SessionId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AccessToken);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SteamLoginSecure);
            hashCode = hashCode * -1521134295 + SteamId.GetHashCode();

            return hashCode;
        }

        /// <summary>
        ///     Determines whether this instance holds enough information to be considered as a valid representation of a
        ///     logged in session.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance holds enough information; otherwise, <c>false</c>.
        /// </returns>
        public override bool HasEnoughInfo()
        {
            return base.HasEnoughInfo() &&
                   SteamId > 0;
        }

        /// <summary>
        ///     Refreshes the Steam session. Necessary to perform confirmations if your session has expired or changed.
        /// </summary>
        /// <returns>true if the operation completed successfully; otherwise false</returns>
        public async Task<bool> RefreshSession(SteamMobileWebAccess mobileWebAccess)
        {
            if (string.IsNullOrWhiteSpace(RefreshToken))
            {
                return false;
            }

            try
            {
                var serverResponse = await OperationRetryHelper.Default.RetryOperationAsync(
                    () => new SteamWebAPI(mobileWebAccess)
                        .RequestObject<SteamWebAPIResponse<GenerateAccessTokenForAppResponse>>(
                            "IAuthenticationService",
                            SteamWebAccessRequestMethod.Post,
                            "GenerateAccessTokenForApp",
                            "v1",
                            new
                            {
                                refresh_token = RefreshToken,
                                steamid = SteamId
                            }
                        )
                ).ConfigureAwait(false);

                if (serverResponse == null)
                {
                    throw new TokenInvalidException();
                }

                if (string.IsNullOrEmpty(serverResponse?.Response?.AccessToken))
                {
                    return false;
                }

                AccessToken = serverResponse.Response.AccessToken;
                SteamLoginSecure = $"{SteamId}%7C%7C{AccessToken}";

                return true;
            }
            catch (Exception e)
            {
                if (IsTokenExpired(e))
                {
                    throw new TokenInvalidException(e);
                }
            }

            return false;
        }

        public static bool IsTokenExpired(Exception exception)
        {
            HttpWebResponse response = null;
            switch (exception)
            {
                case TokenInvalidException _:
                    return true;
                case WebException webException:
                    response = webException.Response as HttpWebResponse;
                    break;
                case AggregateException aggregateException:
                {
                    foreach (var e in aggregateException.InnerExceptions.OfType<WebException>())
                    {
                        response = e.Response as HttpWebResponse;
                        break;
                    }

                    break;
                }
                default:
                    return false;
            }
            
            // Redirecting -- likely to a steammobile:// URI
            if (response?.StatusCode != HttpStatusCode.Found)
            {
                return false;
            }

            var location = response.Headers.Get("Location");
            if (string.IsNullOrEmpty(location))
            {
                return false;
            }

            // Our OAuth token has expired. This is given both when we must refresh our session, or the entire OAuth Token cannot be refreshed anymore.
            // Thus, we should only throw this exception when we're attempting to refresh our session.
            return location.StartsWith("steammobile://lostauth", StringComparison.CurrentCultureIgnoreCase);
        }

        public bool IsExpired()
        {
            var token = AccessToken.Split('.').FirstOrDefault()?.Replace('-', '+').Replace('_', '/');
            if (string.IsNullOrEmpty(token))
            {
                return true;
            }

            if (token.Length % 4 != 0)
            {
                token += new string('=', 4 - token.Length % 4);
            }

            var payload = JsonConvert.DeserializeObject<AccessTokenPayload>(
                System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(token)
                )
            );

            if (payload == null)
            {
                return true;
            }

            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.Expiry;
        }

        public void UpdateSession(WebSession webSession)
        {
            SessionId = webSession.SessionId;
            SteamLoginSecure = webSession.SteamLoginSecure;

            if (webSession is MobileSession mobileSession)
            {
                AccessToken = mobileSession.AccessToken;
                RefreshToken = mobileSession.RefreshToken;
            }
        }
    }
}