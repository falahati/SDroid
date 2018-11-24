using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace SDroid.SteamWeb
{
    /// <summary>
    ///     Represents a web session
    /// </summary>
    /// <seealso cref="System.IEquatable{WebSession}" />
    public class WebSession : IEquatable<WebSession>
    {
        public const string CommunityCookieDomain = ".steamcommunity.com";

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebSession" /> class.
        /// </summary>
        /// <param name="steamLogin">The steam user login.</param>
        /// <param name="steamLoginSecure">The steam user login secure.</param>
        /// <param name="sessionId">The session identifier string.</param>
        [JsonConstructor]
        // ReSharper disable once TooManyDependencies
        public WebSession(
            string steamLogin,
            string steamLoginSecure,
            string sessionId) : this()
        {
            WebCookies.Add(new Cookie("Steam_Language", SteamWebAccess.SteamLanguage, "/",
                CommunityCookieDomain));
            WebCookies.Add(new Cookie("dob", "", "/", CommunityCookieDomain));

            SteamLogin = steamLogin;
            SteamLoginSecure = steamLoginSecure;
            SessionId = sessionId;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebSession" /> class.
        /// </summary>
        public WebSession()
        {
            WebCookies = new CookieContainer();
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
        ///     Gets the web session remember login token
        /// </summary>
        public string RememberLoginToken
        {
            get => WebCookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["steamRememberLogin"]?.Value;
            protected set =>
                WebCookies.Add(new Cookie("steamRememberLogin", value ?? "", "/", CommunityCookieDomain));
        }

        /// <summary>
        ///     Gets the web session identifier string.
        /// </summary>
        public string SessionId
        {
            get => WebCookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["sessionid"]?.Value;
            protected set => WebCookies.Add(new Cookie("sessionid", value ?? "", "/", CommunityCookieDomain));
        }

        /// <summary>
        ///     Gets the steam user login.
        /// </summary>
        public string SteamLogin
        {
            get => WebCookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["steamLogin"]?.Value;
            protected set => WebCookies.Add(new Cookie("steamLogin", value ?? "", "/", CommunityCookieDomain)
            {
                HttpOnly = true
            });
        }

        /// <summary>
        ///     Gets the steam user login secure.
        /// </summary>
        public string SteamLoginSecure
        {
            get => WebCookies?.GetCookies(new Uri(SteamWebAccess.CommunityBaseUrl))["steamLoginSecure"]?.Value;
            protected set => WebCookies.Add(
                new Cookie("steamLoginSecure", value ?? "", "/", CommunityCookieDomain)
                {
                    HttpOnly = true,
                    Secure = true
                });
        }

        protected CookieContainer WebCookies { get; }

        /// <inheritdoc />
        public virtual bool Equals(WebSession other)
        {
            return other != null &&
                   SessionId == other.SessionId &&
                   SteamLogin == other.SteamLogin &&
                   SteamLoginSecure == other.SteamLoginSecure;
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
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SteamLogin);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SteamLoginSecure);

            return hashCode;
        }

        public virtual WebSession Clone()
        {
            return new WebSession(SteamLogin, SteamLoginSecure, SessionId);
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
                   (!string.IsNullOrWhiteSpace(SteamLogin) ||
                    !string.IsNullOrWhiteSpace(SteamLoginSecure));
        }
    }
}