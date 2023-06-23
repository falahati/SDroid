using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamMobile.Exceptions;
using SDroid.SteamMobile.Models.MobileConfigurationsJson;
using SDroid.SteamMobile.Models.TwoFactorServiceAPI;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Models;

namespace SDroid.SteamMobile
{
    /// <summary>
    ///     Represents an Authenticator software or device for generating Steam guard codes as a two factor authentication
    ///     response or to accept or deny new confirmation requests
    /// </summary>
    public class Authenticator
    {
        private const string MobileConfirmationDetailUrl =
            SteamWebAccess.CommunityBaseUrl + "/mobileconf/details/{0:D}";

        private const string MobileConfirmationOperationsUrl =
            SteamWebAccess.CommunityBaseUrl + "/mobileconf/ajaxop";

        private const string MobileConfirmationsOperationsUrl =
            SteamWebAccess.CommunityBaseUrl + "/mobileconf/multiajaxop";

        private const string MobileConfirmationsUrl = SteamWebAccess.CommunityBaseUrl + "/mobileconf/getlist";
        protected const long SteamGuardCodeGenerationStep = 30L;
        protected const int SteamGuardCodeLength = 5;
        
        protected static readonly char[] SteamGuardCodeTranslations =
        {
            '2', '3', '4', '5', '6', '7', '8', '9', 'B', 'C',
            'D', 'F', 'G', 'H', 'J', 'K', 'M', 'N', 'P', 'Q',
            'R', 'T', 'V', 'W', 'X', 'Y'
        };

        protected readonly SteamMobileWebAccess SteamWeb;

        [JsonConstructor]
        public Authenticator(AddAuthenticatorResponse authenticatorData, MobileSession session, string deviceId)
        {
            AuthenticatorData = authenticatorData;
            Session = session;
            DeviceId = deviceId;
            SteamWeb = new SteamMobileWebAccess(session ?? new MobileSession());
        }

        /// <summary>
        ///     Gets the authenticator data representing a valid registered authenticator.
        /// </summary>
        /// <value>
        ///     The authenticator data.
        /// </value>
        public AddAuthenticatorResponse AuthenticatorData { get; }

        /// <summary>
        ///     Gets the device identifier associated with the authenticator
        /// </summary>
        /// <value>
        ///     The device identifier of the associated authenticator.
        /// </value>
        public string DeviceId { get; }

        /// <summary>
        ///     Gets the session data required to access Steam network.
        /// </summary>
        /// <value>
        ///     The logged-in session data of the associated user account.
        /// </value>
        public MobileSession Session { get; }

        /// <summary>
        ///     Returns a new instance of Authenticator class based on the description provided by the passed serialized string in
        ///     Json format
        /// </summary>
        /// <param name="serialized">The serialized representation of an Authenticator instance as string in Json format.</param>
        /// <returns>An instance of Authenticator class</returns>
        public static Authenticator DeSerialize(string serialized)
        {
            var authenticator = JsonConvert.DeserializeObject<Authenticator>(serialized);

            // Tries to fill authenticator data by searching for properties in the root of Json object
            if (authenticator.AuthenticatorData == null || !authenticator.AuthenticatorData.HasEnoughInfo())
            {
                var authenticatorData = JsonConvert.DeserializeObject<AddAuthenticatorResponse>(serialized);
                authenticator = new Authenticator(
                    authenticatorData,
                    authenticator.Session,
                    authenticator.DeviceId
                );
            }

            // Tries to fill session data by searching for properties in the root of Json object
            if (authenticator.Session == null || !authenticator.Session.HasEnoughInfo())
            {
                var sessionData = JsonConvert.DeserializeObject<MobileSession>(serialized);
                if (sessionData?.HasEnoughInfo() == true)
                {
                    authenticator = new Authenticator(
                        authenticator.AuthenticatorData,
                        sessionData,
                        authenticator.DeviceId
                    );
                }
            }

            // Tries to extract steam guard machine authentication tokens
            if (authenticator.Session != null && !(authenticator.Session.SteamMachineAuthenticationTokens?.Count > 0))
            {
                var steamIdProperties = JsonConvert.DeserializeAnonymousType(
                    serialized,
                    new
                    {
                        steamid = (ulong?) null,
                        steam_id = (ulong?) null,
                        session = new
                        {
                            steamid = (ulong?) null,
                            steam_id = (ulong?) null
                        }
                    }
                );
                var steamId = steamIdProperties.steam_id ??
                              steamIdProperties.steamid ??
                              steamIdProperties.session.steam_id ??
                              steamIdProperties.session.steamid ?? authenticator.Session.SteamId;

                var webCookieProperties = JsonConvert.DeserializeAnonymousType(
                    serialized,
                    new
                    {
                        webcookie = (string) null,
                        web_cookie = (string) null,
                        session = new
                        {
                            webcookie = (string) null,
                            web_cookie = (string) null
                        }
                    }
                );
                var webCookie = webCookieProperties.web_cookie ??
                                webCookieProperties.webcookie ??
                                webCookieProperties.session.web_cookie ?? webCookieProperties.session.webcookie;

                if (steamId != null && !string.IsNullOrWhiteSpace(webCookie))
                {
                    var newSession = new MobileSession(
                        authenticator.Session.OAuthToken,
                        steamId,
                        authenticator.Session.SteamLogin,
                        authenticator.Session.SteamLoginSecure,
                        authenticator.Session.SessionId,
                        authenticator.Session.RememberLoginToken,
                        new Dictionary<ulong, string>
                        {
                            { steamId.Value, webCookie }
                        }
                    );

                    authenticator = new Authenticator(
                        authenticator.AuthenticatorData,
                        newSession,
                        authenticator.DeviceId
                    );
                }
            }

            // Tries to fill device identification string by searching for properties in the root of Json object
            if (string.IsNullOrWhiteSpace(authenticator.DeviceId))
            {
                var deviceIdProperties = JsonConvert.DeserializeAnonymousType(
                    serialized,
                    new
                    {
                        deviceId = (string) null,
                        device_id = (string) null,
                        device = (string) null
                    }
                );
                authenticator = new Authenticator(
                    authenticator.AuthenticatorData,
                    authenticator.Session,
                    deviceIdProperties.device_id ?? deviceIdProperties.deviceId ?? deviceIdProperties.device
                );
            }

            // Do we have a enough information to call this a valid instance?
            return authenticator.HasEnoughInfo() ? authenticator : null;
        }

        /// <summary>
        ///     Returns a new instance of Authenticator class based on the description provided by the passed serialized file name
        ///     in Json format
        /// </summary>
        /// <param name="fileName">The Json file to read serialized string from.</param>
        /// <returns>An instance of Authenticator class</returns>
        public static Authenticator DeSerializeFromFile(string fileName)
        {
            return DeSerialize(File.ReadAllText(fileName));
        }

        /// <summary>
        ///     Generates and returns a new steam guard code using the passed shared secret.
        /// </summary>
        /// <param name="sharedSecret">The shared secret to generate steam guard code from.</param>
        /// <returns>
        ///     The newly generated steam guard code.
        /// </returns>
        public static async Task<string> GenerateSteamGuardCode(string sharedSecret)
        {
            return GenerateSteamGuardCodeForTime(sharedSecret, await SteamTime.GetTime().ConfigureAwait(false));
        }

        /// <summary>
        ///     Generates and returns a new steam guard code using the passed shared secret.
        /// </summary>
        /// <param name="sharedSecret">The shared secret to generate steam guard code from.</param>
        /// <returns>
        ///     The newly generated steam guard code.
        /// </returns>
        public static async Task<string> GenerateSteamGuardCode(byte[] sharedSecret)
        {
            return GenerateSteamGuardCodeForTime(sharedSecret, await SteamTime.GetTime().ConfigureAwait(false));
        }

        /// <summary>
        ///     Generates and returns a new steam guard code based on the time windows passed as an argument using the passed
        ///     shared secret
        /// </summary>
        /// <param name="sharedSecret">The shared secret to generate steam guard code from.</param>
        /// <param name="time">The time window to generate the code for.</param>
        /// <returns>The newly generated steam guard code.</returns>
        public static string GenerateSteamGuardCodeForTime(string sharedSecret, DateTime time)
        {
            if (string.IsNullOrEmpty(sharedSecret))
            {
                return "";
            }

            var sharedSecretUnEscaped = Regex.Unescape(sharedSecret);
            var sharedSecretArray = Convert.FromBase64String(sharedSecretUnEscaped);

            return GenerateSteamGuardCodeForTime(sharedSecretArray, time);
        }

        /// <summary>
        ///     Generates and returns a new steam guard code based on the time windows passed as an argument using the passed
        ///     shared secret
        /// </summary>
        /// <param name="sharedSecret">The shared secret to generate steam guard code from.</param>
        /// <param name="time">The time window to generate the code for.</param>
        /// <returns>The newly generated steam guard code.</returns>
        public static string GenerateSteamGuardCodeForTime(byte[] sharedSecret, DateTime time)
        {
            if (!(sharedSecret?.Length > 0))
            {
                throw new ArgumentException("Shared secret byte array is null or empty.");
            }

            var window = time.ToUnixTime() / SteamGuardCodeGenerationStep;
            var timeArray = BitConverter.GetBytes(window);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(timeArray);
            }

            var hashedData = new HMACSHA1 {Key = sharedSecret}.ComputeHash(timeArray);

            var offset = hashedData[hashedData.Length - 1] & 0x0F;
            var token = ((hashedData[offset] & 0x7f) << 24) |
                        ((hashedData[offset + 1] & 0xff) << 16) |
                        ((hashedData[offset + 2] & 0xff) << 8) |
                        ((hashedData[offset + 3] & 0xff) % 1000000);

            var charArray = new char[SteamGuardCodeLength];

            for (var i = 0; i < charArray.Length; ++i)
            {
                charArray[i] = SteamGuardCodeTranslations[token % SteamGuardCodeTranslations.Length];
                token /= SteamGuardCodeTranslations.Length;
            }

            return new string(charArray);
        }

        /// <summary>
        ///     Accepts one or more confirmations
        /// </summary>
        /// <param name="confirmations">The [array of confirmations/confirmation] to accept.</param>
        /// <returns>true if [all confirmations/the confirmation] successfully accepted; otherwise false.</returns>
        public async Task<bool> AcceptConfirmation(params Confirmation[] confirmations)
        {
            if (confirmations.Length > 1)
            {
                return await ResponseToConfirmations(confirmations, true).ConfigureAwait(false);
            }

            if (confirmations.Length > 0)
            {
                return await ResponseToConfirmation(confirmations[0], true).ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        ///     Denies one or more confirmations
        /// </summary>
        /// <param name="confirmations">The [array of confirmations/confirmation] to deny.</param>
        /// <returns>true if [all confirmations/the confirmation] successfully denied; otherwise false.</returns>
        public async Task<bool> DenyConfirmation(params Confirmation[] confirmations)
        {
            if (confirmations.Length > 1)
            {
                return await ResponseToConfirmations(confirmations, false).ConfigureAwait(false);
            }

            if (confirmations.Length > 0)
            {
                return await ResponseToConfirmation(confirmations[0], false).ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        ///     Retrieves a list of confirmation waiting to be verified by user
        /// </summary>
        /// <returns></returns>
        /// <exception cref="TokenInvalidException">Provided session token is invalid.</exception>
        /// <exception cref="TokenExpiredException">Provided session token has expired.</exception>
        /// <exception cref="WebException">Failed to communicate with steam's network or a bad response received.</exception>
        public async Task<Confirmation[]> FetchConfirmations()
        {
            try
            {
                var parameters = await GetConfirmationParameters("conf").ConfigureAwait(false);
                var response = await OperationRetryHelper.Default.RetryOperationAsync(
                    () => SteamWeb.FetchObject<ConfirmationsResponse>(
                        new SteamWebAccessRequest(
                            MobileConfirmationsUrl,
                            SteamWebAccessRequestMethod.Get,
                            parameters
                        )
                    )
                ).ConfigureAwait(false);

                if (!response.Success)
                {
                    if (response.NeedsAuthentication)
                    {
                        throw new TokenInvalidException();
                    }

                    return Array.Empty<Confirmation>();
                }

                return response.Confirmations.Select(
                    (c) => new Confirmation(
                        c.Id,
                        c.Nonce,
                        c.Type,
                        c.CreatorId,
                        c.Icon,
                        c.Headline,
                        c.Summary.ToArray(),
                        new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc).AddSeconds(c.CreationTime)
                    )
                ).ToArray();
            }
            catch (Exception e)
            {
                if (MobileSession.IsTokenExpired(e))
                {
                    throw new TokenExpiredException(e);
                }

                throw;
            }
        }

        /// <summary>
        ///     Generates and returns a new steam guard code.
        /// </summary>
        /// <returns>The newly generated steam guard code.</returns>
        public async Task<string> GenerateSteamGuardCode()
        {
            return GenerateSteamGuardCodeForTime(await SteamTime.GetTime().ConfigureAwait(false));
        }

        /// <summary>
        ///     Generates and returns a new steam guard code based on the time windows passed as an argument
        /// </summary>
        /// <param name="time">The time window to generate the code for.</param>
        /// <returns>The newly generated steam guard code.</returns>
        public string GenerateSteamGuardCodeForTime(DateTime time)
        {
            return GenerateSteamGuardCodeForTime(AuthenticatorData.SharedSecret, time);
        }

        /// <summary>
        ///     Gets details of a confirmation request as a string in HTML format.
        /// </summary>
        /// <param name="confirmation">The confirmation to get details of.</param>
        /// <returns>The details of confirmation as a string in HTML format; or null if the confirmation is not valid</returns>
        public async Task<string> GetConfirmationDetails(Confirmation confirmation)
        {
            var parameters = await GetConfirmationParameters("details").ConfigureAwait(false);
            var referer = (await GetConfirmationParameters("conf").ConfigureAwait(false)).AppendToUrl(
                MobileConfirmationsUrl);
            var serverResponse = await OperationRetryHelper.Default.RetryOperationAsync(
                () => SteamWeb.FetchObject<ConfirmationDetailsResponse>(
                    new SteamWebAccessRequest(
                        string.Format(MobileConfirmationDetailUrl, confirmation.Id),
                        SteamWebAccessRequestMethod.Get,
                        parameters
                    )
                    {
                        Referer = referer
                    }
                )
            ).ConfigureAwait(false);

            if (serverResponse?.Success == true)
            {
                return serverResponse.HTML;
            }

            return null;
        }

        /// <summary>
        ///     Determines whether this instance holds enough information to be considered as a valid representation of a
        ///     registered authenticator device or software.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this instance holds enough information; otherwise, <c>false</c>.
        /// </returns>
        public bool HasEnoughInfo()
        {
            return !string.IsNullOrWhiteSpace(DeviceId) &&
                   Session.HasEnoughInfo() &&
                   AuthenticatorData.HasEnoughInfo();
        }

        /// <summary>
        ///     Revokes this instance and removes it from the user account associated with it.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="RevokeAuthenticatorException">Failed to revoke the authenticator.</exception>
        public async Task RevokeAuthenticator()
        {
            var serverResponse = await OperationRetryHelper.Default.RetryOperationAsync(
                () => SteamWebAPI.Default
                    .RequestObject<SteamWebAPIResponse<RemoveAuthenticatorResponse>>(
                        "ITwoFactorService",
                        SteamWebAccessRequestMethod.Post,
                        "RemoveAuthenticator",
                        "v0001", new
                        {
                            steamid = Session.SteamId,
                            steamguard_scheme = AuthenticatorData.SteamGuardScheme,
                            revocation_code = AuthenticatorData.RevocationCode,
                            access_token = Session.OAuthToken
                        }
                    )
            ).ConfigureAwait(false);

            if (serverResponse?.Response?.Success != true)
            {
                throw new RevokeAuthenticatorException(serverResponse?.Response);
            }
        }

        /// <summary>
        ///     Serializes this instance of Authenticator to a string in Json format
        /// </summary>
        /// <returns>The serialized representation of an Authenticator instance as string in Json format.</returns>
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        ///     Serializes this instance of Authenticator to a string in Json format and saves it to a file
        /// </summary>
        /// <param name="fileName">Name of the Json file to write serialized string to.</param>
        public void SerializeToFile(string fileName)
        {
            var serialized = Serialize();
            File.WriteAllText(fileName, serialized);
        }

        private string GenerateConfirmationHashForTime(DateTime time, string tag)
        {
            var dataArray = BitConverter.GetBytes(time.ToUnixTime());

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(dataArray);
            }

            if (tag != null)
            {
                var tagLen = Math.Min(32, tag.Length);
                Array.Resize(ref dataArray, dataArray.Length + tagLen);
                Array.Copy(Encoding.ASCII.GetBytes(tag), 0, dataArray, dataArray.Length - tagLen, tagLen);
            }

            var hashKey = Convert.FromBase64String(AuthenticatorData.IdentitySecret);

            using (var hmac = new HMACSHA1 {Key = hashKey})
            {
                var dataHash = hmac.ComputeHash(dataArray);
                var encodedDataHash = Convert.ToBase64String(dataHash, Base64FormattingOptions.None);

                return encodedDataHash;
            }
        }

        private async Task<QueryStringBuilder> GetConfirmationParameters(string tag)
        {
            if (string.IsNullOrEmpty(DeviceId))
            {
                throw new ArgumentException("Device Id is not present");
            }

            var time = await SteamTime.GetTime().ConfigureAwait(false);

            return new QueryStringBuilder
            {
                {"p", DeviceId},
                {"a", Session.SteamId},
                {"k", GenerateConfirmationHashForTime(time, tag)},
                {"t", time.ToUnixTime()},
                {"m", MobileSession.ClientName},
                {"tag", tag}
            };
        }

        private async Task<bool> ResponseToConfirmation(Confirmation confirmation, bool allow)
        {
            var operation = allow ? "allow" : "cancel";
            var parameters = await GetConfirmationParameters(operation).ConfigureAwait(false);
            var referer = (await GetConfirmationParameters("conf").ConfigureAwait(false))
                .AppendToUrl(MobileConfirmationsUrl);

            return (await OperationRetryHelper.Default.RetryOperationAsync(
                       () => SteamWeb.FetchObject<SendConfirmationResponse>(
                           new SteamWebAccessRequest(
                               MobileConfirmationOperationsUrl,
                               SteamWebAccessRequestMethod.Get,
                               new QueryStringBuilder
                               {
                                   {"op", operation}
                               }.Concat(
                                   parameters
                               ).Concat(
                                   new QueryStringBuilder
                                   {
                                       {"cid", confirmation.Id},
                                       {"ck", confirmation.Key}
                                   }
                               )
                           )
                           {
                               Referer = referer
                           }
                       )
                   ).ConfigureAwait(false))?.Success ==
                   true;
        }

        private async Task<bool> ResponseToConfirmations(Confirmation[] confirmations, bool allow)
        {
            var operation = allow ? "allow" : "cancel";
            var parameters = await GetConfirmationParameters(operation).ConfigureAwait(false);
            var referer = (await GetConfirmationParameters("conf").ConfigureAwait(false))
                .AppendToUrl(MobileConfirmationsUrl);

            return (
                await OperationRetryHelper.Default.RetryOperationAsync(
                    () => SteamWeb.FetchObject<SendConfirmationResponse>(
                        new SteamWebAccessRequest(
                            MobileConfirmationsOperationsUrl,
                            SteamWebAccessRequestMethod.Post,
                            new QueryStringBuilder
                            {
                                { "op", operation }
                            }.Concat(
                                parameters
                            ).Concat(
                                confirmations.SelectMany(confirmation => new QueryStringBuilder
                                {
                                    { "cid[]", confirmation.Id },
                                    { "ck[]", confirmation.Key }
                                })
                            )
                        )
                        {
                            Referer = referer
                        }
                    )
                ).ConfigureAwait(false)
            )?.Success == true;
        }
    }
}