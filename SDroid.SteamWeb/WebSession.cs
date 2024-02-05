using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
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
        private string _webCookie;
        public const string CommunityCookieDomain = ".steamcommunity.com";
        public const string StoreCookieDomain = "store.steampowered.com";


        /// <summary>
        ///     Initializes a new instance of the <see cref="WebSession" /> class.
        /// </summary>
        /// <param name="steamId">The steam user identifier number.</param>
        /// <param name="steamLoginSecure">The steam secure login token.</param>
        /// <param name="sessionId">The session identifier string.</param>
        /// <param name="webCookie">The web cookie value</param>
        [JsonConstructor]
        // ReSharper disable once TooManyDependencies
        public WebSession(
            ulong steamId,
            string steamLoginSecure,
            string sessionId,
            string webCookie
            ) : this()
        {
            SteamId = steamId;
            SteamLoginSecure = steamLoginSecure;
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N").ToUpper() : sessionId;
            WebCookie = webCookie;
        }

        public WebSession(LoginResponseTransferParameters transferParameters, string sessionId) : this(
            transferParameters.SteamId,
            $"{transferParameters.SteamId}%7C%7C{transferParameters.TokenSecure}",
            sessionId,
            transferParameters.WebCookie
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
        ///     Gets or sets the webcoockie.
        /// </summary>
        public string WebCookie
        {
            get => _webCookie;
            protected set => _webCookie = value;
        }
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="WebSession" /> class.
        /// </summary>
        public WebSession()
        {
            Cookies = new CookieContainer();
            AddCookie("Steam_Language", SteamWebAccess.SteamLanguage);
            AddCookie("dob", "");
            SessionId = Guid.NewGuid().ToString("N").ToUpper();
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
            Cookies = cookieContainer;
        }
        
        /// <summary>
        ///     Gets the web session identifier string.
        /// </summary>
        public string SessionId
        {
            get => Cookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["sessionid"]?.Value;
            protected set => AddCookie("sessionid", value ?? "");
        }
        
        /// <summary>
        ///     Gets the steam user login secure.
        /// </summary>
        public string SteamLoginSecure
        {
            get => Cookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["steamLoginSecure"]?.Value;
            protected set => AddCookie("steamLoginSecure", value ?? "", true, true);
        }

        [JsonIgnore]
        protected CookieContainer Cookies { get; }

        /// <inheritdoc />
        public virtual bool Equals(WebSession other)
        {
            return other != null &&
                   SessionId == other.SessionId &&
                   SteamId == other.SteamId &&
                   SteamLoginSecure == other.SteamLoginSecure;
        }

        public static bool operator ==(WebSession data1, WebSession data2)
        {
            return Equals(data1, data2) || data1?.Equals(data2) == true;
        }

        public static implicit operator CookieContainer(WebSession session)
        {
            return session.Cookies;
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
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SteamId.ToString());
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SteamLoginSecure);

            return hashCode;
        }

        public virtual WebSession Clone()
        {
            return new WebSession(SteamId, SteamLoginSecure, SessionId, WebCookie);
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
                   !string.IsNullOrWhiteSpace(SteamLoginSecure);
        }

        public CookieContainer AddCookie(string name, string value, bool httpOnly = false, bool secure = false)
        {
            Cookies.Add(
                new Cookie(name, value, "/", CommunityCookieDomain)
                {
                    HttpOnly = httpOnly
                }
            );
            Cookies.Add(
                new Cookie(name, value, "/", StoreCookieDomain)
                {
                    HttpOnly = httpOnly
                }
            );
            return Cookies;
        }
    }
}