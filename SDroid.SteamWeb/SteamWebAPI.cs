using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SDroid.SteamWeb
{
    public class SteamWebAPI
    {
        internal const string ApiKeyRegisterUrl = SteamWebAccess.CommunityBaseUrl + "/dev/registerkey";
        internal const string ApiKeyRevokeUrl = SteamWebAccess.CommunityBaseUrl + "/dev/revokekey";
        internal const string ApiKeyUrl = SteamWebAccess.CommunityBaseUrl + "/dev/apikey";
        public const string SteamAPIBaseUrl = "https://api.steampowered.com";
        private static readonly Uri SteamAPIBaseUri = new Uri(SteamAPIBaseUrl);
        private string _apiKey;

        public SteamWebAPI() : this(null)
        {
        }

        public SteamWebAPI(string apiKey) : this(apiKey, SteamWebAccess.GetGuest())
        {
        }

        public SteamWebAPI(string apiKey, SteamWebAccess steamWebAccess)
        {
            _apiKey = apiKey;
            SteamWebAccess = steamWebAccess;
        }


        public string ApiKey
        {
            get => _apiKey;
            set => _apiKey = !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Null or empty API key provided.", nameof(value));
        }

        public static SteamWebAPI Default { get; } = new SteamWebAPI();

        protected SteamWebAccess SteamWebAccess { get; }

        public static async Task<string> GetApiKey(SteamWebAccess steamWebAccess)
        {
            var serverResponse = await OperationRetryHelper.Default.RetryOperationAsync(
                () => steamWebAccess.FetchString(
                    new SteamWebAccessRequest(
                        ApiKeyUrl,
                        SteamWebAccessRequestMethod.Get,
                        null
                    )
                    {
                        Referer = SteamWebAccess.CommunityBaseUrl
                    }
                )
            ).ConfigureAwait(false);

            if (serverResponse.Contains(@"/dev/registerkey"))
            {
                return null;
            }

            var match = Regex.Match(serverResponse, @"<p>Key: (.+)</p>", RegexOptions.IgnoreCase);

            if (match.Groups.Count > 0 && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        public static async Task<bool> RegisterApiKey(SteamWebAccess steamWebAccess, string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                throw new ArgumentException("Invalid domain name or ip address provided.", nameof(domainName));
            }

            var serverResponse = await OperationRetryHelper.Default.RetryOperationAsync(
                () => steamWebAccess.FetchString(
                    new SteamWebAccessRequest(
                        ApiKeyRegisterUrl,
                        SteamWebAccessRequestMethod.Post,
                        QueryStringBuilder.FromDynamic(new
                        {
                            domain = domainName,
                            agreeToTerms = "agreed",
                            sessionid = steamWebAccess.Session.SessionId
                        })
                    )
                    {
                        Referer = ApiKeyUrl
                    }
                )
            ).ConfigureAwait(false);

            return serverResponse?.Contains(@"/dev/registerkey") == true;
        }


        public static async Task<bool> RevokeApiKey(SteamWebAccess steamWebAccess, string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                throw new ArgumentException("Invalid domain name or ip address provided.", nameof(domainName));
            }

            var serverResponse = await OperationRetryHelper.Default.RetryOperationAsync(
                () => steamWebAccess.FetchString(
                    new SteamWebAccessRequest(
                        ApiKeyRevokeUrl,
                        SteamWebAccessRequestMethod.Post,
                        QueryStringBuilder.FromDynamic(new
                        {
                            sessionid = steamWebAccess.Session.SessionId
                        })
                    )
                    {
                        Referer = ApiKeyUrl
                    }
                )
            ).ConfigureAwait(false);

            return serverResponse?.Contains(@"/dev/revokekey") == true;
        }

        // ReSharper disable once TooManyArguments
        public virtual Task<dynamic> RequestDynamic(
            string interfaceName,
            SteamWebAccessRequestMethod method,
            string function,
            string version = "v1",
            dynamic arguments = null)
        {
            return SteamWebAccess.FetchDynamic(
                MakeRequest(
                    interfaceName,
                    method,
                    function,
                    version,
                    QueryStringBuilder.FromDynamic(arguments) as QueryStringBuilder
                )
            );
        }

        // ReSharper disable once TooManyArguments
        public virtual async Task<T> RequestObject<T>(
            string interfaceName,
            SteamWebAccessRequestMethod method,
            string function,
            string version = "v1",
            dynamic arguments = null)
        {
            return await SteamWebAccess.FetchObject<T>(
                MakeRequest(
                    interfaceName,
                    method,
                    function,
                    version,
                    QueryStringBuilder.FromDynamic(arguments) as QueryStringBuilder
                )
            ).ConfigureAwait(false);
        }

        // ReSharper disable once TooManyArguments
        public virtual Task<string> RequestString(
            string interfaceName,
            SteamWebAccessRequestMethod method,
            string function,
            string version = "v1",
            dynamic arguments = null)
        {
            return SteamWebAccess.FetchString(
                MakeRequest(
                    interfaceName,
                    method,
                    function,
                    version,
                    QueryStringBuilder.FromDynamic(arguments) as QueryStringBuilder
                )
            );
        }

        // ReSharper disable once MethodTooLong
        // ReSharper disable once TooManyArguments
        protected virtual SteamWebAccessRequest MakeRequest(
            string interfaceName,
            SteamWebAccessRequestMethod requestMethod,
            string functionName,
            string functionVersion,
            QueryStringBuilder functionArguments)
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
            {
                throw new ArgumentException("Interface name is empty or invalid.");
            }

            if (string.IsNullOrWhiteSpace(functionName))
            {
                throw new ArgumentException("Function name is empty or invalid.");
            }

            functionVersion = !string.IsNullOrWhiteSpace(functionVersion) ? functionVersion : "v1";

            var url = new Uri(SteamAPIBaseUri,
                string.Format("{0}/{1}/{2}/", interfaceName, functionName, functionVersion)).AbsoluteUri;

            if (functionArguments != null)
            {
                if (interfaceName.EndsWith("Service"))
                {
                    functionArguments = new QueryStringBuilder
                    {
                        {
                            "input_json",
                            JsonConvert.SerializeObject(
                                functionArguments.ToDictionary(pair => pair.Key, pair => pair.Value))
                        }
                    };
                }
            }

            var queryString = new QueryStringBuilder
            {
                {"language", "en_us"}
            };

            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                queryString.Add("key", ApiKey);
            }

            if (requestMethod == SteamWebAccessRequestMethod.Get)
            {
                functionArguments = queryString.Concat(functionArguments);
            }
            else
            {
                url = queryString.AppendToUrl(url);
            }

            return new SteamWebAccessRequest(
                url,
                requestMethod,
                functionArguments
            )
            {
                Referer = SteamAPIBaseUrl,
                AcceptFailureResponses = true
            };
        }
    }
}