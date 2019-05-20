using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamTrade.EventArguments;
using SDroid.SteamTrade.Exceptions;
using SDroid.SteamTrade.InternalModels.EconomyServiceAPI;
using SDroid.SteamTrade.InternalModels.InventoryJson;
using SDroid.SteamTrade.InternalModels.TradeOfferJson;
using SDroid.SteamTrade.Models.Trade;
using SDroid.SteamTrade.Models.TradeOffer;
using SDroid.SteamTrade.Models.UserInventory;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Models;
using SteamKit2;

namespace SDroid.SteamTrade
{
    // ReSharper disable once ClassTooBig
    // ReSharper disable once HollowTypeName
    public class TradeOfferManager : IDisposable
    {
        private const string TradeOfferAcceptUrl = TradeOfferUrl + "/accept";
        private const string TradeOfferCancelUrl = TradeOfferUrl + "/cancel";
        private const string TradeOfferDeclineUrl = TradeOfferUrl + "/decline";
        internal const string TradeOfferNewUrl = SteamWebAccess.CommunityBaseUrl + "/tradeoffer/new";

        private const string TradeOfferPartnerInventoryUrl =
            SteamWebAccess.CommunityBaseUrl + "/tradeoffer/new/partnerinventory/";

        private const string TradeOfferSendUrl = TradeOfferNewUrl + "/send";
        private const string TradeOfferUrl = SteamWebAccess.CommunityBaseUrl + "/tradeoffer/{0}";
        internal static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly JsonSerializerSettings JsonSerializerSettings =
            new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                Formatting = Formatting.None
            };

        public event EventHandler<TradeOfferStateChangedEventArgs> TradeOfferAccepted;
        public event EventHandler<TradeOfferStateChangedEventArgs> TradeOfferCanceled;

        public event EventHandler<TradeOfferStateChangedEventArgs> TradeOfferChanged;
        public event EventHandler<TradeOfferStateChangedEventArgs> TradeOfferDeclined;

        public event EventHandler<TradeOfferStateChangedEventArgs> TradeOfferInEscrow;

        public event EventHandler<TradeOfferStateChangedEventArgs> TradeOfferNeedsConfirmation;

        public event EventHandler<TradeOfferStateChangedEventArgs> TradeOfferReceived;
        public event EventHandler<TradeOfferStateChangedEventArgs> TradeOfferSent;

        private readonly Dictionary<long, TradeOfferStatus> _knownTradeOffers =
            new Dictionary<long, TradeOfferStatus>();

        private readonly Queue<TradeOffer> _unhandledTradeOfferUpdates = new Queue<TradeOffer>();
        private bool _isHandlingTradeOffers;

        private Timer _timer;

        private TradeOfferOptions _tradeOfferOptions;

        public TradeOfferManager(
            SteamWebAPI steamWebAPI,
            SteamWebAccess steamWebAccess,
            TradeOfferOptions tradeOfferOptions)
        {
            SteamWebAccess = steamWebAccess ?? throw new ArgumentNullException(nameof(steamWebAccess));
            SteamWebAPI = steamWebAPI ?? throw new ArgumentNullException(nameof(steamWebAPI));
            _tradeOfferOptions = tradeOfferOptions ?? TradeOfferOptions.Default;
        }


        public DateTime? LastUpdate { get; private set; }

        private SteamWebAccess SteamWebAccess { get; }

        private SteamWebAPI SteamWebAPI { get; }


        public TradeOfferOptions TradeOfferOptions
        {
            get => _tradeOfferOptions;
            set => _tradeOfferOptions = value ?? TradeOfferOptions.Default;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _timer?.Dispose();
        }

        internal static async Task<TradeOffer> GetTradeOffer(
            SteamWebAPI steamWebAPI,
            long offerId,
            OperationRetryHelper retryHelperOptions)
        {
            var response = await retryHelperOptions.RetryOperationAsync(
                () => steamWebAPI.RequestObject<SteamWebAPIResponse<GetTradeOfferResponse>>(
                    "IEconService",
                    SteamWebAccessRequestMethod.Get,
                    "GetTradeOffer",
                    "v1",
                    new
                    {
                        tradeofferid = offerId.ToString(),
                        language = "en_us",
                        get_descriptions = 1
                    }
                ),
                r => Task.FromResult(r?.Response?.Offer?.IsValid() == true)
            ).ConfigureAwait(false);

            if (response?.Response?.Offer?.IsValid() != true)
            {
                throw new TradeOfferException("Failed to retrieve trade offer.");
            }

            return new TradeOffer(response.Response.Offer, response.Response?.Descriptions?.ToArray());
        }

        // ReSharper disable once TooManyArguments
        // ReSharper disable once ExcessiveIndentation
        // ReSharper disable once MethodTooLong
        private static async Task<UserAppInventory> GetTradeOfferPartnerInventory(
            SteamWebAccess steamWebAccess,
            SteamID partnerSteamId,
            long appId,
            long contextId,
            OperationRetryHelper retryHelperOptions)
        {
            var startPosition = 0;
            var assets = new List<UserInventoryAsset>();
            var assetDescriptions = new List<UserInventoryAssetDescription>();
            var appInfos = new List<UserInventoryApp>();

            do
            {
                var retrySuccess = false;

                try
                {
                    var positionCopy = startPosition;
                    var response = await retryHelperOptions.RetryOperationAsync(
                        () => steamWebAccess.FetchObject<InventoryResponseV1>(
                            new SteamWebAccessRequest(
                                TradeOfferPartnerInventoryUrl,
                                SteamWebAccessRequestMethod.Get,
                                positionCopy > 0
                                    ? QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = steamWebAccess.Session.SessionId,
                                        partner = partnerSteamId.ConvertToUInt64(),
                                        appid = appId,
                                        contextid = contextId,
                                        start = positionCopy
                                    })
                                    : QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = steamWebAccess.Session.SessionId,
                                        partner = partnerSteamId.ConvertToUInt64(),
                                        appid = appId,
                                        contextid = contextId
                                    })
                            )
                            {
                                Referer = TradeOfferNewUrl
                            }
                        ),
                        r => Task.FromResult(r?.Success == true)
                    ).ConfigureAwait(false);

                    if (response?.Success == true)
                    {
                        retrySuccess = true;

                        foreach (var inventoryAsset in response.Assets)
                        {
                            var steamInventoryAsset = inventoryAsset.Value.ToSteamInventoryAsset(appId, contextId);

                            if (!assets.Contains(steamInventoryAsset))
                            {
                                assets.Add(steamInventoryAsset);
                            }
                        }

                        foreach (var itemDescription in response.Descriptions)
                        {
                            var steamAssetDescription = itemDescription.Value.ToSteamAssetDescription();

                            if (!assetDescriptions.Contains(steamAssetDescription))
                            {
                                assetDescriptions.Add(steamAssetDescription);
                            }
                        }

                        foreach (var app in response.Apps ?? new InventoryAppInfoV1[0])
                        {
                            appInfos.Add(app.ToSteamInventoryAsset());
                        }

                        if (!response.More)
                        {
                            return new UserAppInventory(assets.ToArray(), assetDescriptions.ToArray(),
                                appInfos.ToArray());
                        }

                        startPosition = response.MoreStart;
                    }
                }
                catch (WebException)
                {
                    // ignored
                }

                if (!retrySuccess)
                {
                    throw new UserInventoryFetchAssetsException(appId, contextId, partnerSteamId);
                }

                await Task.Delay(retryHelperOptions.RequestDelay).ConfigureAwait(false);
            } while (true);
        }

        // ReSharper disable once TooManyDeclarations
        // ReSharper disable once TooManyArguments
        private static async Task<UserInventoryApp[]> GetTradeOfferPartnerInventoryOverview(
            SteamWebAccess steamWebAccess,
            SteamID partnerSteamId,
            string token,
            OperationRetryHelper retryHelperOptions)
        {
            var regex = new Regex("var g_rgPartnerAppContextData = (.*?);");
            var inventoryPageResponse = await retryHelperOptions.RetryOperationAsync(() =>
                    steamWebAccess.FetchString(
                        new SteamWebAccessRequest(
                            TradeOfferNewUrl,
                            SteamWebAccessRequestMethod.Get,
                            !string.IsNullOrWhiteSpace(token)
                                ? new QueryStringBuilder
                                {
                                    {"partner", partnerSteamId.AccountID},
                                    {"token", token}
                                }
                                : new QueryStringBuilder
                                {
                                    {"partner", partnerSteamId.AccountID}
                                }
                        )
                        {
                            Referer = SteamWebAccess.CommunityBaseUrl
                        }
                    ),
                s => Task.FromResult(!string.IsNullOrWhiteSpace(s) && regex.Match(s).Success),
                false
            ).ConfigureAwait(false);

            var matched = !string.IsNullOrWhiteSpace(inventoryPageResponse) ? regex.Match(inventoryPageResponse) : null;

            if (matched?.Success != true ||
                matched.Groups.Count < 1 ||
                !matched.Groups[1].Success ||
                string.IsNullOrWhiteSpace(matched.Groups[1].Value))
            {
                throw new UserInventoryFetchOverviewException(partnerSteamId);
            }

            try
            {
                var overviewResult =
                    JsonConvert.DeserializeObject<Dictionary<long, InventoryApp>>(matched.Groups[1].Value);

                return overviewResult?.Values.Select(app => app.ToUserInventoryApp()).ToArray();
            }
            catch (Exception e)
            {
                throw new UserInventoryFetchOverviewException(partnerSteamId, e);
            }
        }

        public async Task<long?> Accept(TradeOffer offer)
        {
            if (offer.IsOurOffer ||
                offer.Status != TradeOfferStatus.Active)
            {
                throw new InvalidOperationException("Can't accept a trade that is not active and/or not ours.");
            }

            var response = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAccess.FetchObject<TradeOfferAcceptResponse>(
                    new SteamWebAccessRequest(
                        string.Format(TradeOfferAcceptUrl, offer.TradeOfferId),
                        SteamWebAccessRequestMethod.Post,
                        QueryStringBuilder.FromDynamic(new
                        {
                            sessionid = SteamWebAccess.Session.SessionId,
                            tradeofferid = offer.TradeOfferId,
                            serverid = "1"
                        })
                    )
                    {
                        Referer = string.Format(TradeOfferUrl, offer.TradeOfferId)
                    }
                ),
                shouldThrowExceptionOnTotalFailure: false
            ).ConfigureAwait(false);

            if (response?.IsAccepted == true && response.TradeId != null)
            {
                return response.TradeId;
            }

            var refreshedOffer = await GetTradeOffer(offer.TradeOfferId).ConfigureAwait(false);

            if (refreshedOffer?.Status == TradeOfferStatus.Accepted && refreshedOffer.TradeId != null)
            {
                return refreshedOffer.TradeId;
            }

            if (response?.IsAccepted == true || refreshedOffer?.Status == TradeOfferStatus.Accepted)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(response?.Error))
            {
                throw new TradeOfferException(response.Error);
            }

            throw new TradeOfferException("Failed to accept trade offer.");
        }

        public async Task Cancel(TradeOffer offer)
        {
            if (!offer.IsOurOffer ||
                offer.Status != TradeOfferStatus.Active)
            {
                throw new InvalidOperationException("Can't cancel a trade that is not active and/or not ours.");
            }

            var response = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAccess.FetchObject<TradeOfferAcceptResponse>(
                    new SteamWebAccessRequest(
                        string.Format(TradeOfferCancelUrl, offer.TradeOfferId),
                        SteamWebAccessRequestMethod.Post,
                        QueryStringBuilder.FromDynamic(new
                        {
                            sessionid = SteamWebAccess.Session.SessionId,
                            tradeofferid = offer.TradeOfferId,
                            serverid = "1"
                        })
                    )
                    {
                        Referer = string.Format(TradeOfferUrl, offer.TradeOfferId)
                    }
                ),
                shouldThrowExceptionOnTotalFailure: false
            ).ConfigureAwait(false);

            if (response?.TradeOfferId == offer.TradeOfferId ||
                (await GetTradeOffer(offer.TradeOfferId).ConfigureAwait(false))?.Status == TradeOfferStatus.Canceled)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(response?.Error))
            {
                throw new TradeOfferException(response.Error);
            }

            throw new TradeOfferException("Failed to cancel trade offer.");
        }

        public async Task CancelTradeOfferAlternate(TradeOffer offer)
        {
            if (!offer.IsOurOffer ||
                offer.Status != TradeOfferStatus.Active)
            {
                throw new InvalidOperationException("Can't cancel a trade that is not active and/or not ours.");
            }

            var response = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAPI.RequestObject<CancelTradeOfferResponse>(
                    "IEconService",
                    SteamWebAccessRequestMethod.Post,
                    "CancelTradeOffer",
                    "v1",
                    new
                    {
                        tradeofferid = offer.TradeOfferId
                    }
                ),
                shouldThrowExceptionOnTotalFailure: false
            ).ConfigureAwait(false);

            if (response?.Success == true ||
                (await GetTradeOffer(offer.TradeOfferId).ConfigureAwait(false)).Status ==
                TradeOfferStatus.Canceled)
            {
                return;
            }

            throw new TradeOfferException("Failed to cancel trade offer.");
        }

        public async Task<long> CounterOffer(
            TradeOffer oldOffer,
            NewTradeOfferItemsList offerItems,
            string newMessage)
        {
            if (oldOffer.IsOurOffer ||
                oldOffer.Status != TradeOfferStatus.Active)
            {
                throw new InvalidOperationException(
                    "Can't counter offer a trade that that is not active and/or is ours.");
            }

            var response = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAccess.FetchObject<TradeOfferCreateResponse>(
                    new SteamWebAccessRequest(
                        TradeOfferSendUrl,
                        SteamWebAccessRequestMethod.Post,
                        QueryStringBuilder.FromDynamic(new
                        {
                            sessionid = SteamWebAccess.Session.SessionId,
                            serverid = "1",
                            partner = oldOffer.PartnerSteamId.ConvertToUInt64().ToString(),
                            tradeoffermessage = newMessage,
                            json_tradeoffer =
                                JsonConvert.SerializeObject(offerItems.AsTradeOfferStatus(2),
                                    JsonSerializerSettings),
                            tradeofferid_countered = oldOffer.TradeOfferId,
                            trade_offer_create_params = "{}"
                        })
                    )
                    {
                        Referer = string.Format(TradeOfferUrl, oldOffer.TradeOfferId)
                    }
                )
            ).ConfigureAwait(false);

            if (response?.TradeOfferId != null ||
                (await GetTradeOffer(oldOffer.TradeOfferId).ConfigureAwait(false)).Status ==
                TradeOfferStatus.Countered)
            {
                if (response?.TradeOfferId != null)
                {
                    return response.TradeOfferId.Value;
                }

                return 0;
            }

            if (!string.IsNullOrWhiteSpace(response?.TradeError))
            {
                throw new TradeOfferException(response.TradeError);
            }

            throw new TradeOfferException("Failed to send counter offer.");
        }

        public async Task Decline(TradeOffer offer)
        {
            if (offer.IsOurOffer ||
                offer.Status != TradeOfferStatus.Active)
            {
                throw new InvalidOperationException("Can't decline a trade that is not active and/or is ours.");
            }

            var response = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAccess.FetchObject<TradeOfferDeclineResponse>(
                    new SteamWebAccessRequest(
                        string.Format(TradeOfferDeclineUrl, offer.TradeOfferId),
                        SteamWebAccessRequestMethod.Post,
                        QueryStringBuilder.FromDynamic(new
                        {
                            sessionid = SteamWebAccess.Session.SessionId,
                            tradeofferid = offer.TradeOfferId.ToString(),
                            serverid = "1"
                        })
                    )
                    {
                        Referer = string.Format(TradeOfferUrl, offer.TradeOfferId)
                    }
                )
            ).ConfigureAwait(false);

            if (response?.TradeOfferId == offer.TradeOfferId ||
                (await GetTradeOffer(offer.TradeOfferId).ConfigureAwait(false))?.Status ==
                TradeOfferStatus.Declined)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(response?.Error))
            {
                throw new TradeOfferException(response.Error);
            }

            throw new TradeOfferException("Failed to decline trade offer.");
        }

        public async Task DeclineTradeOfferAlternate(TradeOffer offer)
        {
            if (offer.IsOurOffer ||
                offer.Status != TradeOfferStatus.Active)
            {
                throw new InvalidOperationException("Can't decline a trade that is not active and/or is ours.");
            }

            var response = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAPI.RequestObject<DeclineTradeOfferResponse>(
                    "IEconService",
                    SteamWebAccessRequestMethod.Post,
                    "DeclineTradeOffer",
                    "v1",
                    new
                    {
                        tradeofferid = offer.TradeOfferId
                    }
                )
            ).ConfigureAwait(false);

            if (response?.Success == true ||
                (await GetTradeOffer(offer.TradeOfferId).ConfigureAwait(false))?.Status ==
                TradeOfferStatus.Declined)
            {
                return;
            }

            throw new TradeOfferException("Failed to decline trade offer.");
        }

        public Task<EscrowDuration> GetEscrowDuration(SteamID partnerSteamId)
        {
            return GetEscrowDuration(partnerSteamId, null);
        }

        // ReSharper disable once TooManyDeclarations
        public async Task<EscrowDuration> GetEscrowDuration(SteamID partnerSteamId, string token)
        {
            var serverResponse = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAccess.FetchString(
                    new SteamWebAccessRequest(
                        TradeOfferNewUrl,
                        SteamWebAccessRequestMethod.Get,
                        string.IsNullOrWhiteSpace(token)
                            ? QueryStringBuilder.FromDynamic(new
                            {
                                partner = partnerSteamId.AccountID
                            })
                            : QueryStringBuilder.FromDynamic(new
                            {
                                partner = partnerSteamId.AccountID,
                                token
                            })
                    )
                    {
                        Referer = SteamWebAccess.CommunityBaseUrl
                    }
                ),
                shouldThrowExceptionOnTotalFailure: false
            ).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(serverResponse))
            {
                var myMatch = Regex.Match(serverResponse, @"g_daysMyEscrow(?:[\s=]+)(?<days>[\d]+);",
                    RegexOptions.IgnoreCase);
                var theirMatch = Regex.Match(serverResponse, @"g_daysTheirEscrow(?:[\s=]+)(?<days>[\d]+);",
                    RegexOptions.IgnoreCase);
                var bothMatch = Regex.Match(serverResponse, @"g_daysBothEscrow(?:[\s=]+)(?<days>[\d]+);",
                    RegexOptions.IgnoreCase);

                if (myMatch.Groups["days"].Success &&
                    double.TryParse(myMatch.Groups["days"].Value, out var myEscrowInDays) &&
                    theirMatch.Groups["days"].Success &&
                    double.TryParse(myMatch.Groups["days"].Value, out var theirEscrowInDays))
                {
                    if (bothMatch.Groups["days"].Success &&
                        double.TryParse(myMatch.Groups["days"].Value, out var bothEscrowInDays))
                    {
                        myEscrowInDays = bothEscrowInDays;
                        theirEscrowInDays = bothEscrowInDays;
                    }

                    return new EscrowDuration(TimeSpan.FromDays(myEscrowInDays), TimeSpan.FromDays(theirEscrowInDays));
                }
            }

            throw new EscrowDurationException();
        }

        // ReSharper disable once TooManyDeclarations
        public async Task<EscrowDuration> GetEscrowDuration(TradeOffer tradeOffer)
        {
            var serverResponse = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAccess.FetchString(
                    new SteamWebAccessRequest(
                        string.Format(TradeOfferUrl, tradeOffer.TradeOfferId),
                        SteamWebAccessRequestMethod.Get,
                        null
                    )
                    {
                        Referer = SteamWebAccess.CommunityBaseUrl
                    }
                ),
                shouldThrowExceptionOnTotalFailure: false
            ).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(serverResponse))
            {
                var myMatch = Regex.Match(serverResponse, @"g_daysMyEscrow(?:[\s=]+)(?<days>[\d]+);",
                    RegexOptions.IgnoreCase);
                var theirMatch = Regex.Match(serverResponse, @"g_daysTheirEscrow(?:[\s=]+)(?<days>[\d]+);",
                    RegexOptions.IgnoreCase);
                var bothMatch = Regex.Match(serverResponse, @"g_daysBothEscrow(?:[\s=]+)(?<days>[\d]+);",
                    RegexOptions.IgnoreCase);

                if (myMatch.Groups["days"].Success &&
                    double.TryParse(myMatch.Groups["days"].Value, out var myEscrowInDays) &&
                    theirMatch.Groups["days"].Success &&
                    double.TryParse(myMatch.Groups["days"].Value, out var theirEscrowInDays))
                {
                    if (bothMatch.Groups["days"].Success &&
                        double.TryParse(myMatch.Groups["days"].Value, out var bothEscrowInDays))
                    {
                        myEscrowInDays = bothEscrowInDays;
                        theirEscrowInDays = bothEscrowInDays;
                    }

                    return new EscrowDuration(TimeSpan.FromDays(myEscrowInDays), TimeSpan.FromDays(theirEscrowInDays));
                }
            }

            throw new EscrowDurationException();
        }

        // ReSharper disable once TooManyDeclarations
        public async Task<UserInventory> GetPartnerInventory(SteamID tradeOfferPartner, string tradeOfferToken)
        {
            var overview = await _tradeOfferOptions.RetryOperationAsync(
                () => GetTradeOfferPartnerInventoryOverview(
                    SteamWebAccess,
                    tradeOfferPartner,
                    tradeOfferToken,
                    _tradeOfferOptions
                ),
                shouldThrowExceptionOnTotalFailure: false
            ).ConfigureAwait(false);

            return new UserInventory(
                SteamWebAccess,
                tradeOfferPartner,
                overview,
                (access, id, appId, contextId) =>
                    GetTradeOfferPartnerInventory(access, id, appId, contextId, _tradeOfferOptions)
            );
        }

        public Task<UserInventory> GetPartnerInventory(SteamID tradeOfferPartner)
        {
            return GetPartnerInventory(tradeOfferPartner, null);
        }

        public Task<TradeReceipt> GetReceipt(TradeOffer offer)
        {
            if (offer.Status != TradeOfferStatus.Accepted || !(offer.TradeId > 0))
            {
                throw new InvalidOperationException("Can't get a receipt for a trade offer that is not yet accepted.");
            }

            try
            {
                return TradeManager.GetReceipt(TradeOfferOptions, SteamWebAccess, offer.TradeId.Value);
            }
            catch (TradeException e)
            {
                throw new TradeOfferException(e.Message, e);
            }
        }

        public Task<TradeOffer> GetTradeOffer(long offerId)
        {
            return GetTradeOffer(SteamWebAPI, offerId, _tradeOfferOptions);
        }

        // ReSharper disable once TooManyArguments
        // ReSharper disable once TooManyDeclarations
        public async Task<TradeOffer[]> GetTradeOffers(
            // ReSharper disable once FlagArgument
            bool getSentOffers,
            // ReSharper disable once FlagArgument
            bool getReceivedOffers,
            bool getDescriptions,
            bool activeOnly,
            bool historicalOnly,
            DateTime? timeHistoricalCutoff = null)
        {
            if (!getSentOffers && !getReceivedOffers)
            {
                throw new ArgumentException("getSentOffers and getReceivedOffers can't be both false");
            }

            if (timeHistoricalCutoff == null)
            {
                timeHistoricalCutoff = Epoch.AddSeconds(1389106496);
            }

            var response = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAPI.RequestObject<SteamWebAPIResponse<GetTradeOffersResponse>>(
                    "IEconService",
                    SteamWebAccessRequestMethod.Get,
                    "GetTradeOffers",
                    "v1",
                    new
                    {
                        get_sent_offers = getSentOffers ? 1 : 0,
                        get_received_offers = getReceivedOffers ? 1 : 0,
                        get_descriptions = getDescriptions ? 1 : 0,
                        language = "en_us",
                        active_only = activeOnly ? 1 : 0,
                        historical_only = historicalOnly ? 1 : 0,
                        time_historical_cutoff = ((int) (timeHistoricalCutoff.Value - Epoch).TotalSeconds).ToString()
                    }
                )
            ).ConfigureAwait(false);

            var economyTradeOffersResponse = response?.Response ?? new GetTradeOffersResponse();

            return economyTradeOffersResponse.AllOffers.Where(offer => offer.IsValid()).Select(offer =>
                new TradeOffer(offer, economyTradeOffersResponse.Descriptions?.ToArray())).ToArray();
        }


        public async Task<TradeOffersSummary> GetTradeOffersSummary(DateTime since)
        {
            var response = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAPI.RequestObject<SteamWebAPIResponse<TradeOffersSummaryResponse>>(
                    "IEconService",
                    SteamWebAccessRequestMethod.Get,
                    "GetTradeOffersSummary",
                    "v1",
                    new
                    {
                        time_last_visit = (ulong) (since.ToUniversalTime() - Epoch).TotalSeconds
                    }
                )
            ).ConfigureAwait(false);

            return new TradeOffersSummary(response?.Response ?? new TradeOffersSummaryResponse());
        }

        public async Task<long> Send(
            SteamID partnerSteamId,
            NewTradeOfferItemsList offerItemsList,
            string newMessage)
        {
            var response = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAccess.FetchObject<TradeOfferCreateResponse>(
                    new SteamWebAccessRequest(
                        TradeOfferSendUrl,
                        SteamWebAccessRequestMethod.Post,
                        QueryStringBuilder.FromDynamic(new
                        {
                            sessionid = SteamWebAccess.Session.SessionId,
                            serverid = "1",
                            partner = partnerSteamId.ConvertToUInt64().ToString(),
                            tradeoffermessage = newMessage,
                            json_tradeoffer =
                                JsonConvert.SerializeObject(offerItemsList.AsTradeOfferStatus(),
                                    JsonSerializerSettings),
                            trade_offer_create_params = "{}"
                        })
                    )
                    {
                        Referer = new QueryStringBuilder
                        {
                            {"partner", partnerSteamId.AccountID}
                        }.AppendToUrl(TradeOfferNewUrl)
                    }
                )
            ).ConfigureAwait(false);

            if (response?.TradeOfferId != null)
            {
                return response.TradeOfferId.Value;
            }

            if (!string.IsNullOrWhiteSpace(response?.TradeError))
            {
                throw new TradeOfferException(response.TradeError);
            }

            throw new TradeOfferException("Failed to send trade offer.");
        }

        // ReSharper disable once TooManyArguments
        public async Task<long> SendTradeOfferWithToken(
            SteamID partnerSteamId,
            string token,
            NewTradeOfferItemsList offerItemsList,
            string newMessage)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Partner trade offer token is missing or invalid.", nameof(token));
            }

            var offerToken = new TradeOfferCreateParameters {TradeOfferAccessToken = token};

            var response = await _tradeOfferOptions.RetryOperationAsync(
                () => SteamWebAccess.FetchObject<TradeOfferCreateResponse>(
                    new SteamWebAccessRequest(
                        TradeOfferSendUrl,
                        SteamWebAccessRequestMethod.Post,
                        QueryStringBuilder.FromDynamic(new
                        {
                            sessionid = SteamWebAccess.Session.SessionId,
                            serverid = "1",
                            partner = partnerSteamId.ConvertToUInt64().ToString(),
                            tradeoffermessage = newMessage,
                            json_tradeoffer =
                                JsonConvert.SerializeObject(offerItemsList.AsTradeOfferStatus(),
                                    JsonSerializerSettings),
                            trade_offer_create_params = JsonConvert.SerializeObject(offerToken, JsonSerializerSettings)
                        })
                    )
                    {
                        Referer = new QueryStringBuilder
                        {
                            {"partner", partnerSteamId.AccountID},
                            {"token", token}
                        }.AppendToUrl(TradeOfferNewUrl)
                    }
                )
            ).ConfigureAwait(false);

            if (response?.TradeOfferId != null)
            {
                return response.TradeOfferId.Value;
            }

            if (!string.IsNullOrWhiteSpace(response?.TradeError))
            {
                throw new TradeOfferException(response.TradeError);
            }

            throw new TradeOfferException("Failed to send trade offer.");
        }

        public void StartPolling()
        {
            if (_timer != null)
            {
                return;
            }

            _timer = new Timer(Callback, null, _tradeOfferOptions.PollInterval, TimeSpan.FromMilliseconds(-1));
        }

        public Task<bool> ValidateAccess(SteamID partnerSteamId)
        {
            return ValidateAccess(partnerSteamId, null);
        }

        public async Task<bool> ValidateAccess(SteamID partnerSteamId, string token)
        {
            try
            {
                var serverResponse = await _tradeOfferOptions.RetryOperationAsync(
                    () => SteamWebAccess.FetchString(
                        new SteamWebAccessRequest(
                            TradeOfferNewUrl,
                            SteamWebAccessRequestMethod.Get,
                            string.IsNullOrWhiteSpace(token)
                                ? QueryStringBuilder.FromDynamic(new
                                {
                                    partner = partnerSteamId.AccountID
                                })
                                : QueryStringBuilder.FromDynamic(new
                                {
                                    partner = partnerSteamId.AccountID,
                                    token
                                })
                        )
                        {
                            Referer = SteamWebAccess.CommunityBaseUrl
                        }
                    )
                ).ConfigureAwait(false);

                return serverResponse.ToLower()
                    .Contains("g_ulTradePartnerSteamID = '".ToLower() + partnerSteamId.ConvertToUInt64());
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        protected virtual bool OnTradeOfferAccepted(TradeOfferStateChangedEventArgs eventArgs)
        {
            try
            {
                TradeOfferAccepted?.Invoke(this, eventArgs);

                return eventArgs.Processed;
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool OnTradeOfferCanceled(TradeOfferStateChangedEventArgs eventArgs)
        {
            try
            {
                TradeOfferCanceled?.Invoke(this, eventArgs);

                return eventArgs.Processed;
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool OnTradeOfferChanged(TradeOfferStateChangedEventArgs eventArgs)
        {
            try
            {
                TradeOfferChanged?.Invoke(this, eventArgs);

                return eventArgs.Processed;
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool OnTradeOfferDeclined(TradeOfferStateChangedEventArgs eventArgs)
        {
            try
            {
                TradeOfferDeclined?.Invoke(this, eventArgs);

                return eventArgs.Processed;
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool OnTradeOfferInEscrow(TradeOfferStateChangedEventArgs eventArgs)
        {
            try
            {
                TradeOfferInEscrow?.Invoke(this, eventArgs);

                return eventArgs.Processed;
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool OnTradeOfferNeedsConfirmation(TradeOfferStateChangedEventArgs eventArgs)
        {
            try
            {
                TradeOfferNeedsConfirmation?.Invoke(this, eventArgs);

                return eventArgs.Processed;
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool OnTradeOfferReceived(TradeOfferStateChangedEventArgs eventArgs)
        {
            try
            {
                TradeOfferReceived?.Invoke(this, eventArgs);

                return eventArgs.Processed;
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool OnTradeOfferSent(TradeOfferStateChangedEventArgs eventArgs)
        {
            try
            {
                TradeOfferSent?.Invoke(this, eventArgs);

                return eventArgs.Processed;
            }
            catch
            {
                return false;
            }
        }

        private async void Callback(object state)
        {
            try
            {
                var startTime = DateTime.UtcNow;

                TradeOffer[] offers;

                if (LastUpdate == null)
                {
                    offers = await GetTradeOffers(true, true, true, true, false).ConfigureAwait(false);
                }
                else
                {
                    offers = await GetTradeOffers(
                            true, true, false, true, false,
                            LastUpdate.Value - TimeSpan.FromMinutes(5))
                        .ConfigureAwait(false);
                }

                LastUpdate = startTime;

                if (offers != null)
                {
                    lock (_unhandledTradeOfferUpdates)
                    {
                        foreach (var offer in offers)
                        {
                            _unhandledTradeOfferUpdates.Enqueue(offer);
                        }
                    }
                }

                var _ = Task.Run(() => HandlePendingTradeOffers());
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                try
                {
                    _timer.Change(_tradeOfferOptions.PollInterval, TimeSpan.FromMilliseconds(-1));
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void HandlePendingTradeOffers()
        {
            lock (_timer)
            {
                if (_isHandlingTradeOffers)
                {
                    return;
                }

                _isHandlingTradeOffers = true;
            }

            while (true)
            {
                TradeOffer offer;

                lock (_unhandledTradeOfferUpdates)
                {
                    if (!_unhandledTradeOfferUpdates.Any())
                    {
                        break;
                    }

                    offer = _unhandledTradeOfferUpdates.Dequeue();
                }

                try
                {
                    var offerId = offer.TradeOfferId;

                    lock (_knownTradeOffers)
                    {
                        if (_knownTradeOffers.ContainsKey(offerId))
                        {
                            if (_knownTradeOffers[offerId] == offer.Status)
                            {
                                continue;
                            }

                            _knownTradeOffers.Remove(offerId);
                        }
                    }

                    if (!OnTradeOfferChanged(new TradeOfferStateChangedEventArgs(offer)))
                    {
                        return;
                    }

                    switch (offer.Status)
                    {
                        case TradeOfferStatus.Active:

                            if (offer.IsOurOffer)
                            {
                                if (!OnTradeOfferSent(new TradeOfferStateChangedEventArgs(offer)))
                                {
                                    return;
                                }
                            }
                            else
                            {
                                if (!OnTradeOfferReceived(new TradeOfferStateChangedEventArgs(offer)))
                                {
                                    return;
                                }
                            }

                            break;
                        case TradeOfferStatus.Accepted:
                            if (!OnTradeOfferAccepted(new TradeOfferStateChangedEventArgs(offer)))
                            {
                                return;
                            }

                            break;
                        case TradeOfferStatus.Expired:

                            if (offer.IsOurOffer)
                            {
                                if (!OnTradeOfferCanceled(new TradeOfferStateChangedEventArgs(offer)))
                                {
                                    return;
                                }
                            }
                            else
                            {
                                if (!OnTradeOfferDeclined(new TradeOfferStateChangedEventArgs(offer)))
                                {
                                    return;
                                }
                            }

                            break;
                        case TradeOfferStatus.Canceled:
                            if (!OnTradeOfferCanceled(new TradeOfferStateChangedEventArgs(offer)))
                            {
                                return;
                            }

                            break;
                        case TradeOfferStatus.Countered:
                        case TradeOfferStatus.Declined:
                            if (!OnTradeOfferDeclined(new TradeOfferStateChangedEventArgs(offer)))
                            {
                                return;
                            }

                            break;
                        case TradeOfferStatus.NeedsConfirmation:
                            if (!OnTradeOfferNeedsConfirmation(new TradeOfferStateChangedEventArgs(offer)))
                            {
                                return;
                            }

                            break;
                        case TradeOfferStatus.CanceledBySecondFactor:
                            if (!OnTradeOfferCanceled(new TradeOfferStateChangedEventArgs(offer)))
                            {
                                return;
                            }

                            break;
                        case TradeOfferStatus.InEscrow:

                            if (!OnTradeOfferInEscrow(new TradeOfferStateChangedEventArgs(offer)))
                            {
                                return;
                            }

                            break;
                    }

                    lock (_knownTradeOffers)
                    {
                        _knownTradeOffers[offerId] = offer.Status;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            lock (_timer)
            {
                _isHandlingTradeOffers = false;
            }
        }
    }
}