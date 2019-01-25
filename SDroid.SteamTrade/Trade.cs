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
using SDroid.SteamTrade.InternalModels.InventoryJson;
using SDroid.SteamTrade.InternalModels.TradeJson;
using SDroid.SteamTrade.InternalModels.TradeJson.Constants;
using SDroid.SteamTrade.Models.Trade;
using SDroid.SteamTrade.Models.UserInventory;
using SDroid.SteamWeb;
using SteamKit2;

namespace SDroid.SteamTrade
{
    /// <summary>
    ///     Class which represents a trade.
    ///     Note that the logic that Steam uses can be seen from their web-client source-code:
    ///     http://steamcommunity-a.akamaihd.net/public/javascript/economy_trade.js
    /// </summary>
    // ReSharper disable once ClassTooBig
    public class Trade : IDisposable
    {
        private const string TradeAddItemUrl = TradeUrl + "/additem";
        private const string TradeCancelUrl = TradeUrl + "/cancel";
        private const string TradeChatUrl = TradeUrl + "/chat";
        private const string TradeConfirmUrl = TradeUrl + "/confirm";
        private const string TradePartnerInventoryUrl = TradeUrl + "/foreigninventory";
        private const string TradeRemoveItemUrl = TradeUrl + "/removeitem";
        private const string TradeStatusUrl = TradeUrl + "/tradestatus";
        private const string TradeToggleReadyUrl = TradeUrl + "/toggleready";
        internal const string TradeUrl = SteamWebAccess.CommunityBaseUrl + "/trade/{0}";

        private readonly List<Asset> _myOfferedItems = new List<Asset>();
        private readonly Dictionary<int, Asset> _myOfferedItemsLocalCopy = new Dictionary<int, Asset>();
        private readonly List<Asset> _partnerOfferedItems = new List<Asset>();
        private readonly List<TradeEvent> _processedEventList = new List<TradeEvent>();
        private readonly SteamWebAccess _steamWebAccess;
        private readonly TradeOptions _tradeOptions;
        private readonly SemaphoreSlim _userActionLockObject = new SemaphoreSlim(1, 1);
        private int _failedTradeStates;
        private int _logPosition;
        private Timer _timer;
        private CancellationTokenSource _tokenSource;
        private int _version;
        public event EventHandler<PartnerAcceptedEventArgs> PartnerAccepted;
        public event EventHandler<PartnerMessagedEventArgs> PartnerMessaged;
        public event EventHandler<PartnerOfferedItemsChangedEventArgs> PartnerOfferedItemsChanged;
        public event EventHandler<PartnerReadyStateChangedEventArgs> PartnerReadyStateChanged;
        public event EventHandler<PartnerStatusChangedEventArgs> PartnerStatusChanged;
        public event EventHandler<TradeEndedEventArgs> TradeEnded;
        public event EventHandler<TradeTimedOutEventArgs> TradeTimedOut;

        internal Trade(
            SteamID other,
            SteamWebAccess steamWebAccess,
            TradeOptions tradeOptions = null)
        {
            PartnerSteamId = other;

            _steamWebAccess = steamWebAccess ?? throw new ArgumentNullException(nameof(steamWebAccess));
            _tradeOptions = tradeOptions ?? TradeOptions.Default;
        }

        public bool IsPartnerAccepted { get; private set; }

        public bool IsPartnerReady { get; private set; }

        public bool IsReady { get; private set; }

        public DateTime? LastPartnerInteraction { get; private set; }

        public DateTime LastVersionChange { get; private set; } = DateTime.Now;

        public Asset[] MyOfferedItems
        {
            get
            {
                lock (_myOfferedItems)
                {
                    return _myOfferedItems.ToArray();
                }
            }
        }

        public Asset[] PartnerOfferedItems
        {
            get
            {
                lock (_partnerOfferedItems)
                {
                    return _partnerOfferedItems.ToArray();
                }
            }
        }

        public TradePartnerStatus PartnerStatus { get; private set; } = TradePartnerStatus.Connecting;

        public SteamID PartnerSteamId { get; }

        public TradeStatus Status { get; private set; } = TradeStatus.Active;

        public DateTime TradeCreated { get; } = DateTime.UtcNow;

        public long? TradeId { get; private set; }

        public Version TradeVersion
        {
            get => new Version(_version, _logPosition);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _userActionLockObject?.Dispose();
            _timer?.Dispose();
            _tokenSource?.Dispose();
        }

        // ReSharper disable once TooManyArguments
        // ReSharper disable once ExcessiveIndentation
        private static async Task<UserAppInventory> GetTradePartnerInventory(
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
                                string.Format(TradePartnerInventoryUrl, partnerSteamId.ConvertToUInt64()),
                                SteamWebAccessRequestMethod.Get,
                                positionCopy > 0
                                    ? QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = steamWebAccess.Session.SessionId,
                                        steamid = partnerSteamId.ConvertToUInt64(),
                                        appid = appId,
                                        contextid = contextId,
                                        start = positionCopy
                                    })
                                    : QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = steamWebAccess.Session.SessionId,
                                        steamid = partnerSteamId.ConvertToUInt64(),
                                        appid = appId,
                                        contextid = contextId
                                    })
                            )
                            {
                                Referer = string.Format(TradeUrl, partnerSteamId.ConvertToUInt64())
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
        private static async Task<UserInventoryApp[]> GetTradePartnerInventoryOverview(
            SteamWebAccess steamWebAccess,
            SteamID partnerSteamId,
            OperationRetryHelper retryHelperOptions)
        {
            var regex = new Regex("var g_rgForeignAppContextData = (.*?);");
            var inventoryPageResponse = await retryHelperOptions.RetryOperationAsync(() =>
                    steamWebAccess.FetchString(
                        new SteamWebAccessRequest(
                            string.Format(TradeUrl, partnerSteamId.ConvertToUInt64()),
                            SteamWebAccessRequestMethod.Get,
                            null
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

        public async Task AcceptTrade()
        {
            if (!IsReady)
            {
                throw new InvalidOperationException("Can't accept a trade if you are not yet ready.");
            }

            if (!VerifyTradeItems())
            {
                throw new InvalidOperationException("Local trade items does not match the steam accepted trade items.");
            }

            await _userActionLockObject.WaitAsync().ConfigureAwait(false);

            if (Status != TradeStatus.Active)
            {
                throw new InvalidOperationException("Trade already ended.");
            }

            CancelTradeStatusPoll();

            try
            {
                await ProcessTradeState(
                    await _tradeOptions.RetryOperationAsync(() =>
                        _steamWebAccess.FetchObject<TradeState>(
                            new SteamWebAccessRequest(
                                string.Format(TradeConfirmUrl, PartnerSteamId.ConvertToUInt64()),
                                SteamWebAccessRequestMethod.Post,
                                QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = _steamWebAccess.Session.SessionId,
                                        version = _version.ToString()
                                    }
                                )
                            )
                        )
                    ).ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await TradeStatusFailure().ConfigureAwait(false);

                throw;
            }
            finally
            {
                _userActionLockObject.Release();
            }
        }

        public async Task AddItem(Asset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            var slot = GetItemSlot(asset);

            if (slot != null)
            {
                throw new InvalidOperationException("Can not add an items twice.");
            }

            slot = NextTradeSlot();

            if (IsReady)
            {
                await SetReadyState(false).ConfigureAwait(false);
            }

            await _userActionLockObject.WaitAsync().ConfigureAwait(false);

            if (Status != TradeStatus.Active)
            {
                throw new InvalidOperationException("Trade already ended.");
            }

            lock (_myOfferedItemsLocalCopy)
            {
                _myOfferedItemsLocalCopy.Add(slot.Value, asset);
            }

            CancelTradeStatusPoll();

            try
            {
                await ProcessTradeState(
                    await _tradeOptions.RetryOperationAsync(() =>
                        _steamWebAccess.FetchObject<TradeState>(
                            new SteamWebAccessRequest(
                                string.Format(TradeAddItemUrl, PartnerSteamId.ConvertToUInt64()),
                                SteamWebAccessRequestMethod.Post,
                                QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = _steamWebAccess.Session.SessionId,
                                        appid = asset.AppId,
                                        contextid = asset.ContextId,
                                        itemid = asset.AssetId,
                                        slot
                                    }
                                )
                            )
                        )
                    ).ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
            catch (Exception)
            {
                lock (_myOfferedItemsLocalCopy)
                {
                    _myOfferedItemsLocalCopy.Remove(slot.Value);
                }

                await TradeStatusFailure().ConfigureAwait(false);

                throw;
            }
            finally
            {
                _userActionLockObject.Release();
            }
        }

        public async Task CancelTrade()
        {
            if (Status != TradeStatus.Active)
            {
                return;
            }

            Status = TradeStatus.Canceled;

            CancelTradeStatusPoll();

            try
            {
                await ProcessTradeState(
                    await _tradeOptions.RetryOperationAsync(() =>
                        _steamWebAccess.FetchObject<TradeState>(
                            new SteamWebAccessRequest(
                                string.Format(TradeCancelUrl, PartnerSteamId.ConvertToUInt64()),
                                SteamWebAccessRequestMethod.Post,
                                QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = _steamWebAccess.Session.SessionId
                                    }
                                )
                            )
                        )
                    ).ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await TradeStatusFailure().ConfigureAwait(false);
            }

            OnTradeEnded(new TradeEndedEventArgs(PartnerSteamId, false, true, false, null, MyOfferedItems,
                PartnerOfferedItems));
        }

        // ReSharper disable once TooManyDeclarations
        public async Task<EscrowDuration> GetEscrowDuration()
        {
            var serverResponse = await _tradeOptions.RetryOperationAsync(
                () => _steamWebAccess.FetchString(
                    new SteamWebAccessRequest(
                        string.Format(TradeUrl, PartnerSteamId.ConvertToUInt64()),
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

        public async Task<UserInventory> GetPartnerInventory()
        {
            UserInventoryApp[] overview = null;

            try
            {
                overview = await GetTradePartnerInventoryOverview(_steamWebAccess, PartnerSteamId, _tradeOptions)
                    .ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored
            }

            return new UserInventory(
                _steamWebAccess,
                PartnerSteamId,
                overview,
                (access, id, appId, contextId) =>
                    GetTradePartnerInventory(access, id, appId, contextId, _tradeOptions)
            );
        }

        public async Task RemoveItem(Asset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            var slot = GetItemSlot(asset);

            if (slot == null)
            {
                throw new InvalidOperationException("Can not remove an item before adding it.");
            }

            if (IsReady)
            {
                await SetReadyState(false).ConfigureAwait(false);
            }

            await _userActionLockObject.WaitAsync().ConfigureAwait(false);

            if (Status != TradeStatus.Active)
            {
                throw new InvalidOperationException("Trade already ended.");
            }

            Asset cachedAsset;

            lock (_myOfferedItemsLocalCopy)
            {
                cachedAsset = _myOfferedItemsLocalCopy[slot.Value];
                _myOfferedItemsLocalCopy.Remove(slot.Value);
            }

            CancelTradeStatusPoll();

            try
            {
                await ProcessTradeState(
                    await _tradeOptions.RetryOperationAsync(() =>
                        _steamWebAccess.FetchObject<TradeState>(
                            new SteamWebAccessRequest(
                                // ReSharper disable once StringLiteralTypo
                                string.Format(TradeRemoveItemUrl, PartnerSteamId.ConvertToUInt64()),
                                SteamWebAccessRequestMethod.Post,
                                QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = _steamWebAccess.Session.SessionId,
                                        appid = asset.AppId,
                                        contextid = asset.ContextId,
                                        itemid = asset.AssetId,
                                        slot
                                    }
                                )
                            )
                        )
                    ).ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
            catch (Exception)
            {
                lock (_myOfferedItemsLocalCopy)
                {
                    _myOfferedItemsLocalCopy.Add(slot.Value, cachedAsset);
                }

                await TradeStatusFailure().ConfigureAwait(false);

                throw;
            }
            finally
            {
                _userActionLockObject.Release();
            }
        }

        public async Task SendMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                throw new ArgumentException("Empty or null message passed.", nameof(msg));
            }

            await _userActionLockObject.WaitAsync().ConfigureAwait(false);

            if (Status != TradeStatus.Active)
            {
                throw new InvalidOperationException("Trade already ended.");
            }

            CancelTradeStatusPoll();

            try
            {
                await ProcessTradeState(
                    await _tradeOptions.RetryOperationAsync(() =>
                        _steamWebAccess.FetchObject<TradeState>(
                            new SteamWebAccessRequest(
                                string.Format(TradeChatUrl, PartnerSteamId.ConvertToUInt64()),
                                SteamWebAccessRequestMethod.Post,
                                QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = _steamWebAccess.Session.SessionId,
                                        message = msg,
                                        logpos = _logPosition.ToString(),
                                        version = _version.ToString()
                                    }
                                )
                            )
                        )
                    ).ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await TradeStatusFailure().ConfigureAwait(false);

                throw;
            }
            finally
            {
                _userActionLockObject.Release();
            }
        }

        // ReSharper disable once FlagArgument
        public async Task SetReadyState(bool isReady)
        {
            await _userActionLockObject.WaitAsync().ConfigureAwait(false);

            if (Status != TradeStatus.Active)
            {
                throw new InvalidOperationException("Trade already ended.");
            }

            if (IsReady == isReady)
            {
                return;
            }

            IsReady = isReady;
            CancelTradeStatusPoll();

            try
            {
                await ProcessTradeState(
                    await _tradeOptions.RetryOperationAsync(() =>
                        _steamWebAccess.FetchObject<TradeState>(
                            new SteamWebAccessRequest(
                                string.Format(TradeToggleReadyUrl, PartnerSteamId.ConvertToUInt64()),
                                SteamWebAccessRequestMethod.Post,
                                QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = _steamWebAccess.Session.SessionId,
                                        ready = isReady ? "true" : "false",
                                        version = _version.ToString()
                                    }
                                )
                            )
                        )
                    ).ConfigureAwait(false)
                ).ConfigureAwait(false);
            }
            catch (Exception)
            {
                IsReady = !isReady;
                await TradeStatusFailure().ConfigureAwait(false);

                throw;
            }
            finally
            {
                _userActionLockObject.Release();
            }
        }

        public bool VerifyTradeItems()
        {
            if (Status != TradeStatus.Active)
            {
                throw new InvalidOperationException("Trade already ended.");
            }

            lock (_myOfferedItems)
            {
                lock (_myOfferedItemsLocalCopy)
                {
                    return _myOfferedItemsLocalCopy.Values
                        .OrderBy(asset => asset.AssetId)
                        .SequenceEqual(_myOfferedItems.OrderBy(asset => asset.AssetId));
                }
            }
        }

        private void CancelTradeStatusPoll()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            _tokenSource?.Cancel();
        }

        private int? GetItemSlot(Asset asset)
        {
            lock (_myOfferedItemsLocalCopy)
            {
                return _myOfferedItemsLocalCopy.Keys
                    .Select(i => (int?) i)
                    .FirstOrDefault(i => _myOfferedItemsLocalCopy[i.Value].AssetId == asset.AssetId);
            }
        }

        // ReSharper disable once MethodTooLong
        // ReSharper disable once ExcessiveIndentation
        private async Task HandleEvents(TradeEvent[] events)
        {
            foreach (var tradeEvent in events.OrderBy(o => o.Timestamp))
            {
                lock (_processedEventList)
                {
                    if (_processedEventList.Contains(tradeEvent))
                    {
                        continue;
                    }

                    //add event to processed list, as we are taking care of this event now
                    _processedEventList.Add(tradeEvent);
                }

                var eventDateTime = TradeOfferManager.Epoch.AddSeconds(tradeEvent.Timestamp);

                var isUs = tradeEvent.SteamId != PartnerSteamId.ConvertToUInt64();

                switch (tradeEvent.Action)
                {
                    case TradeEventType.ItemAdded:
                        var newAsset = tradeEvent.GetAsset();

                        if (newAsset != null)
                        {
                            var added = false;

                            if (isUs)
                            {
                                lock (_myOfferedItems)
                                {
                                    if (!_myOfferedItems.Contains(newAsset))
                                    {
                                        _myOfferedItems.Add(newAsset);
                                        added = true;
                                    }
                                }
                            }
                            else
                            {
                                lock (_partnerOfferedItems)
                                {
                                    if (!_partnerOfferedItems.Contains(newAsset))
                                    {
                                        _partnerOfferedItems.Add(newAsset);
                                        added = true;
                                    }
                                }
                            }

                            if (added)
                            {
                                if (IsReady)
                                {
                                    await SetReadyState(false).ConfigureAwait(false);
                                }

                                if (!isUs)
                                {
                                    LastPartnerInteraction = eventDateTime;
                                    OnPartnerOfferedItemsChanged(
                                        new PartnerOfferedItemsChangedEventArgs(PartnerSteamId,
                                            PartnerOfferedItemsChangedAction.Added, newAsset));
                                }
                            }
                        }

                        break;
                    case TradeEventType.ItemRemoved:
                        var oldAsset = tradeEvent.GetAsset();

                        if (oldAsset != null)
                        {
                            var removed = false;

                            if (isUs)
                            {
                                lock (_myOfferedItems)
                                {
                                    if (_myOfferedItems.Contains(oldAsset))
                                    {
                                        removed = _myOfferedItems.Remove(oldAsset);
                                    }
                                }
                            }
                            else
                            {
                                lock (_partnerOfferedItems)
                                {
                                    if (_partnerOfferedItems.Contains(oldAsset))
                                    {
                                        removed = _partnerOfferedItems.Remove(oldAsset);
                                    }
                                }
                            }

                            if (removed)
                            {
                                if (IsReady)
                                {
                                    await SetReadyState(false).ConfigureAwait(false);
                                }

                                if (!isUs)
                                {
                                    LastPartnerInteraction = eventDateTime;
                                    OnPartnerOfferedItemsChanged(
                                        new PartnerOfferedItemsChangedEventArgs(PartnerSteamId,
                                            PartnerOfferedItemsChangedAction.Removed, oldAsset));
                                }
                            }
                        }

                        break;
                    case TradeEventType.UserSetReady:

                        if (isUs)
                        {
                            if (!IsReady)
                            {
                                await SetReadyState(false).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            LastPartnerInteraction = eventDateTime;

                            if (!IsPartnerReady)
                            {
                                IsPartnerReady = true;
                                OnPartnerReadyStateChanged(
                                    new PartnerReadyStateChangedEventArgs(PartnerSteamId, IsPartnerReady));
                            }
                        }

                        break;
                    case TradeEventType.UserSetUnReady:

                        if (isUs)
                        {
                            if (IsReady)
                            {
                                await SetReadyState(false).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            LastPartnerInteraction = eventDateTime;

                            if (IsPartnerReady)
                            {
                                IsPartnerReady = false;
                                OnPartnerReadyStateChanged(
                                    new PartnerReadyStateChangedEventArgs(PartnerSteamId, IsPartnerReady));
                            }
                        }

                        break;
                    case TradeEventType.UserAccept:

                        if (!isUs)
                        {
                            LastPartnerInteraction = eventDateTime;
                            IsPartnerAccepted = true;
                            OnPartnerAccepted(new PartnerAcceptedEventArgs(PartnerSteamId, MyOfferedItems,
                                PartnerOfferedItems));
                        }

                        break;
                    case TradeEventType.UserChat:

                        if (!isUs)
                        {
                            LastPartnerInteraction = eventDateTime;
                            OnPartnerMessaged(new PartnerMessagedEventArgs(PartnerSteamId, tradeEvent.Text,
                                eventDateTime));
                        }

                        break;
                    // ReSharper disable once RedundantCaseLabel
                    case TradeEventType.ModifiedCurrency:
                    default:
                        await CancelTrade().ConfigureAwait(false);

                        return;
                }
            }

            if (DateTime.Now - (LastPartnerInteraction ?? TradeCreated) > _tradeOptions.TradeTimeOut)
            {
                await CancelTrade().ConfigureAwait(false);
                OnTradeTimedOut(new TradeTimedOutEventArgs(PartnerSteamId, LastPartnerInteraction));
            }
        }

        private async Task HandleTradeVersionChange(TradeState status)
        {
            Asset[] partnerOfferedItemsCopy;

            lock (_partnerOfferedItems)
            {
                partnerOfferedItemsCopy = _partnerOfferedItems.ToArray();
            }

            var partnerOfferedItemsNew = status.Them.GetAssets().Select(t => t.Item2.ToAsset()).ToArray();
            var addedItems = partnerOfferedItemsNew.Except(partnerOfferedItemsCopy).ToArray();
            var removedItems = partnerOfferedItemsCopy.Except(partnerOfferedItemsNew).ToArray();

            if (addedItems.Length > 0 || removedItems.Length > 0)
            {
                LastPartnerInteraction = DateTime.UtcNow;
            }

            lock (_partnerOfferedItems)
            {
                _partnerOfferedItems.Clear();
                _partnerOfferedItems.AddRange(status.Them.GetAssets().Select(t => t.Item2.ToAsset()));
            }

            lock (_myOfferedItems)
            {
                _myOfferedItems.Clear();
                _myOfferedItems.AddRange(status.Me.GetAssets().Select(t => t.Item2.ToAsset()).ToArray());
            }

            foreach (var removedItem in removedItems)
            {
                OnPartnerOfferedItemsChanged(
                    new PartnerOfferedItemsChangedEventArgs(PartnerSteamId,
                        PartnerOfferedItemsChangedAction.Removed, removedItem));
            }

            foreach (var addedItem in addedItems)
            {
                OnPartnerOfferedItemsChanged(
                    new PartnerOfferedItemsChangedEventArgs(PartnerSteamId,
                        PartnerOfferedItemsChangedAction.Added, addedItem));
            }

            if (_version != status.Version)
            {
                _version = status.Version;
                LastVersionChange = DateTime.Now;
            }

            if (IsReady && (removedItems.Length > 0 || addedItems.Length > 0))
            {
                await SetReadyState(false).ConfigureAwait(false);
            }
        }

        private int NextTradeSlot()
        {
            lock (_myOfferedItemsLocalCopy)
            {
                return _myOfferedItemsLocalCopy.Keys
                    .Select(i => i + 1)
                    .FirstOrDefault(i => !_myOfferedItemsLocalCopy.ContainsKey(i));
            }
        }

        private void OnPartnerAccepted(PartnerAcceptedEventArgs eventArgs)
        {
            try
            {
                PartnerAccepted?.Invoke(this, eventArgs);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void OnPartnerMessaged(PartnerMessagedEventArgs eventArgs)
        {
            try
            {
                PartnerMessaged?.Invoke(this, eventArgs);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void OnPartnerOfferedItemsChanged(PartnerOfferedItemsChangedEventArgs eventArgs)
        {
            try
            {
                PartnerOfferedItemsChanged?.Invoke(this, eventArgs);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void OnPartnerReadyStateChanged(PartnerReadyStateChangedEventArgs eventArgs)
        {
            try
            {
                PartnerReadyStateChanged?.Invoke(this, eventArgs);
            }
            catch (Exception)
            {
                // ignored
            }
        }


        private void OnPartnerStatusChanged(PartnerStatusChangedEventArgs eventArgs)
        {
            try
            {
                PartnerStatusChanged?.Invoke(this, eventArgs);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void OnTradeEnded(TradeEndedEventArgs eventArgs)
        {
            try
            {
                TradeEnded?.Invoke(this, eventArgs);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void OnTradeTimedOut(TradeTimedOutEventArgs eventArgs)
        {
            try
            {
                TradeTimedOut?.Invoke(this, eventArgs);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private async Task PollCallback(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            TradeState state = null;

            try
            {
                state = await _tradeOptions.RetryOperationAsync(() =>
                        _steamWebAccess.FetchObject<TradeState>(
                            new SteamWebAccessRequest(
                                string.Format(TradeStatusUrl, PartnerSteamId.ConvertToUInt64()),
                                SteamWebAccessRequestMethod.Post,
                                QueryStringBuilder.FromDynamic(new
                                    {
                                        sessionid = _steamWebAccess.Session.SessionId,
                                        logpos = _logPosition,
                                        version = _version
                                    }
                                )
                            )
                        ),
                    cancellationToken: token
                ).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored
            }

            if (!token.IsCancellationRequested && state != null)
            {
                try
                {
                    await ProcessTradeState(state).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await TradeStatusFailure().ConfigureAwait(false);

                    if (Status == TradeStatus.Active)
                    {
                        QueueNextTradeStatusUpdateRequest();
                    }
                }
            }
            else if (state == null)
            {
                await TradeStatusFailure().ConfigureAwait(false);
            }
        }

        // ReSharper disable once ExcessiveIndentation
        private async Task ProcessTradeState(TradeState state)
        {
            if (state.Success)
            {
                if (state.Status == TradeStateStatus.Completed)
                {
                    Status = TradeStatus.Completed;
                    TradeId = state.TradeId;
                    OnTradeEnded(new TradeEndedEventArgs(PartnerSteamId, true, false, false, TradeId, MyOfferedItems,
                        PartnerOfferedItems));

                    return;
                }

                if (state.Status == TradeStateStatus.PendingConfirmation)
                {
                    Status = TradeStatus.CompletedWaitingForConfirmation;
                    TradeId = state.TradeId;
                    OnTradeEnded(new TradeEndedEventArgs(PartnerSteamId, true, false, true, TradeId, MyOfferedItems,
                        PartnerOfferedItems));

                    return;
                }

                if (state.Status == TradeStateStatus.Canceled)
                {
                    Status = TradeStatus.Canceled;
                    OnTradeEnded(new TradeEndedEventArgs(PartnerSteamId, false, true, false, null, MyOfferedItems,
                        PartnerOfferedItems));

                    return;
                }

                if (state.Status == TradeStateStatus.SessionExpired)
                {
                    Status = TradeStatus.Canceled;
                    OnTradeEnded(new TradeEndedEventArgs(PartnerSteamId, false, true, false, null, MyOfferedItems,
                        PartnerOfferedItems));

                    return;
                }

                if (state.Status == TradeStateStatus.Failed)
                {
                    Status = TradeStatus.Canceled;
                    OnTradeEnded(new TradeEndedEventArgs(PartnerSteamId, false, true, false, null, MyOfferedItems,
                        PartnerOfferedItems));

                    return;
                }

                if (state.Status != TradeStateStatus.OnGoing || state.Me == null || state.Them == null)
                {
                    await TradeStatusFailure().ConfigureAwait(false);

                    return;
                }

                if (state.NewVersion)
                {
                    await HandleTradeVersionChange(state).ConfigureAwait(false);
                }
                //else if (state.Version != Version)
                //{
                //    await TradeStatusFailure(true).ConfigureAwait(false);
                //    return;
                //}

                if (IsPartnerReady != state.Them.Ready)
                {
                    IsPartnerReady = state.Them.Ready;
                    OnPartnerReadyStateChanged(new PartnerReadyStateChangedEventArgs(PartnerSteamId, IsPartnerReady));
                }

                if (state.Them.Confirmed && !IsPartnerAccepted)
                {
                    IsPartnerAccepted = true;
                    OnPartnerAccepted(new PartnerAcceptedEventArgs(PartnerSteamId, MyOfferedItems,
                        PartnerOfferedItems));
                }

                if (!state.Them.IsConnectionPending && state.Them.SecondsSinceTouch < 5)
                {
                    if (PartnerStatus == TradePartnerStatus.Connecting)
                    {
                        PartnerStatus = TradePartnerStatus.InTrade;
                        OnPartnerStatusChanged(new PartnerStatusChangedEventArgs(PartnerSteamId, true, true, false));
                    }
                    else if (PartnerStatus == TradePartnerStatus.Timeout)
                    {
                        PartnerStatus = TradePartnerStatus.InTrade;
                        OnPartnerStatusChanged(new PartnerStatusChangedEventArgs(PartnerSteamId, true, false, false));
                    }
                }
                else
                {
                    if (PartnerStatus == TradePartnerStatus.InTrade)
                    {
                        PartnerStatus = TradePartnerStatus.Timeout;
                        OnPartnerStatusChanged(new PartnerStatusChangedEventArgs(PartnerSteamId, false, false, true));
                    }
                }

                if (!state.Me.Ready && IsReady)
                {
                    await SetReadyState(false).ConfigureAwait(false);
                }
                else if (state.Me.Ready && !IsReady)
                {
                    await SetReadyState(false).ConfigureAwait(false);
                }

                if (state.Events?.Length > 0)
                {
                    await HandleEvents(state.Events).ConfigureAwait(false);
                }

                if (state.LogPosition != 0)
                {
                    _logPosition = state.LogPosition;
                    LastVersionChange = DateTime.Now;
                }

                _failedTradeStates = 0;
                //_lastTradeState = state;
            }
            else
            {
                await TradeStatusFailure().ConfigureAwait(false);
            }

            QueueNextTradeStatusUpdateRequest();
        }

        private void QueueNextTradeStatusUpdateRequest()
        {
            CancelTradeStatusPoll();
            _tokenSource = new CancellationTokenSource(_tradeOptions.PollTimeOut);
            _timer = new Timer(
                async state => await PollCallback(_tokenSource.Token).ConfigureAwait(false),
                null,
                _tradeOptions.PollInterval,
                TimeSpan.FromMilliseconds(-1));
        }

        private async Task TradeStatusFailure()
        {
            _failedTradeStates++;

            if (_failedTradeStates > 3)
            {
                await CancelTrade().ConfigureAwait(false);
            }
        }
    }
}