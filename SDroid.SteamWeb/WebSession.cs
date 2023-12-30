using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using SDroid.SteamWeb.InternalModels;
using SDroid.SteamWeb.Models;

namespace SDroid.SteamWeb
{
    /// <summary>
    ///     Represents a web session
    /// </summary>
    /// <seealso cref="System.IEquatable{WebSession}" />
    public class WebSession : IEquatable<WebSession>
    {
        private ulong _steamCommunityId;
        private string _accessToken;
        public const string CommunityCookieDomain = ".steamcommunity.com";
        public const string StoreCookieDomain = "store.steampowered.com";


        /// <summary>
        ///     Initializes a new instance of the <see cref="WebSession" /> class.
        /// </summary>
        /// <param name="steamId">The steam user identifier number.</param>
        /// <param name="accessToken">The sessions access token.</param>
        /// <param name="sessionId">The session identifier string.</param>
        [JsonConstructor]
        // ReSharper disable once TooManyDependencies
        public WebSession(
            ulong steamId,
            string accessToken,
            string sessionId
            ) : this()
        {
            SessionId = sessionId;
            SteamId = steamId;
            AccessToken = accessToken;
        }

        public WebSession(LoginResponseTransferParameters transferParameters, string sessionId) :
            this(
                transferParameters.SteamId,
                transferParameters.TokenSecure,
                sessionId
            )
        {
        }

        /// <summary>
        ///     Gets the steam user identifier number.
        /// </summary>
        public ulong SteamId
        {
            get => _steamCommunityId;
            protected set => _steamCommunityId = value;
        }

        /// <summary>
        ///     Gets the access token.
        /// </summary>
        public string AccessToken
        {
            get => _accessToken;
            protected set
            {
                _accessToken = value;
                SteamLogin = SteamId + "%7C%7C" + value;
                SteamLoginSecure = SteamId + "%7C%7C" + value;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebSession" /> class.
        /// </summary>
        public WebSession()
        {
            WebCookies = new CookieContainer();
            AddCookie("Steam_Language", SteamWebAccess.SteamLanguage);
            AddCookie("dob", "");
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebSession" /> class.
        /// </summary>
        /// <param name="cookieContainer">
        ///     The cookie container with all required cookies to represent a session. Check
        ///     HasEnoughInfo method afterward to make sure if this is a valid session.
        /// </param>
        public WebSession(CookieContainer cookieContainer)
        {
            WebCookies = cookieContainer;
        }
        
        /// <summary>
        ///     Gets the web session identifier string.
        /// </summary>
        public string SessionId
        {
            get => WebCookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["sessionid"]?.Value;
            protected set => AddCookie("sessionid", value ?? "");
        }

        /// <summary>
        ///     Gets the steam user login.
        /// </summary>
        public string SteamLogin
        {
            get => WebCookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["steamLogin"]?.Value;
            protected set => AddCookie("steamLogin", value ?? "", true);
        }

        /// <summary>
        ///     Gets the steam user login secure.
        /// </summary>
        public string SteamLoginSecure
        {
            get => WebCookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["steamLoginSecure"]?.Value;
            protected set => AddCookie("steamLoginSecure", value ?? "", true, true);
        }
        

        [JsonIgnore]
        protected CookieContainer WebCookies { get; }

        /// <inheritdoc />
        public virtual bool Equals(WebSession other)
        {
            return other != null &&
                   SessionId == other.SessionId &&
                   AccessToken == other.AccessToken;
        }

        public static bool operator ==(WebSession data1, WebSession data2)
        {
            return Equals(data1, data2) || data1?.Equals(data2) == true;
        }

        public static implicit operator CookieContainer(WebSession session)
        {
            return session.WebCookies;
        }

        public static bool operator !=(WebSession data1, WebSession data2)
        {
            return !(data1 == data2);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as WebSession);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hashCode = -823311899;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SessionId);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AccessToken);

            return hashCode;
        }

        public virtual WebSession Clone()
        {
            return new WebSession(SteamId, AccessToken, SessionId);
        }

        /// <summary>
        ///     Determines whether this instance holds enough information to be considered as a valid representation of a
        ///     logged in session.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance holds enough information; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool HasEnoughInfo()
        {
            return !string.IsNullOrWhiteSpace(SessionId) &&
                   !string.IsNullOrWhiteSpace(AccessToken);
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

        public CookieContainer AddCookie(string name, string value, bool httpOnly = false, bool secure = false)
        {
            WebCookies.Add(
                new Cookie(name, value, "/", CommunityCookieDomain)
                {
                    HttpOnly = httpOnly
                }
            );
            WebCookies.Add(
                new Cookie(name, value, "/", StoreCookieDomain)
                {
                    HttpOnly = httpOnly
                }
            );
            return WebCookies;
        }
    }
}