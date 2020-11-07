using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamTrade.Exceptions;
using SDroid.SteamTrade.InternalModels.InventoryJson;
using SDroid.SteamTrade.Models.UserInventory;
using SDroid.SteamWeb;
using SteamKit2;

namespace SDroid.SteamTrade
{
    public class UserInventory : IAssetInventory, IDisposable
    {
        private const string UserInventoryUrl =
            SteamWebAccess.CommunityBaseUrl + "/profiles/{0}/inventory";

        private const string UserInventoryV1Url =
            SteamWebAccess.CommunityBaseUrl + "/profiles/{0}/inventory/json/{1}/{2}/";

        private const string UserInventoryV2Url =
            SteamWebAccess.CommunityBaseUrl + "/inventory/{0}/{1}/{2}/";

        private readonly InventoryRetrieveDelegate _getInventoryDelegate;

        private readonly List<UserInventoryApp> _inventoryApps;

        private readonly Dictionary<long, Dictionary<long, UserAppInventory>> _inventoryCache =
            new Dictionary<long, Dictionary<long, UserAppInventory>>();

        private readonly SemaphoreSlim _lockObject = new SemaphoreSlim(1, 1);

        private readonly SteamWebAccess _steamWebAccess;

        // ReSharper disable once TooManyDependencies
        internal UserInventory(
            SteamWebAccess steamWebAccess,
            SteamID steamId,
            UserInventoryApp[] inventoryApps,
            InventoryRetrieveDelegate getInventoryDelegate)
        {
            _steamWebAccess = steamWebAccess;
            SteamId = steamId;
            _getInventoryDelegate = getInventoryDelegate;
            _inventoryApps = new List<UserInventoryApp>(inventoryApps ?? new UserInventoryApp[0]);
        }

        public UserInventoryApp[] InventoryApps
        {
            get
            {
                lock (_inventoryApps)
                {
                    return _inventoryApps.ToArray();
                }
            }
        }

        /// <inheritdoc />
        async Task<Asset[]> IAssetInventory.GetAssets()
        {
            return (await GetAssets().ConfigureAwait(false)).Cast<Asset>().ToArray();
        }

        public SteamID SteamId { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            _lockObject?.Dispose();
        }

        public static async Task<UserInventory> GetInventory(SteamWebAccess steamWebAccess, SteamID steamId)
        {
            return new UserInventory(
                steamWebAccess,
                steamId,
                await GetInventoryOverview(steamWebAccess, steamId).ConfigureAwait(false),
                GetUserInventory);
        }

        public static async Task<UserInventory> GetInventoryAlternate(SteamWebAccess steamWebAccess, SteamID steamId)
        {
            UserInventoryApp[] overview = null;

            try
            {
                overview = await GetInventoryOverview(steamWebAccess, steamId).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored
            }

            return new UserInventory(
                steamWebAccess,
                steamId,
                overview,
                GetUserInventoryAlternate);
        }

        private static async Task<UserInventoryApp[]> GetInventoryOverview(
            SteamWebAccess steamWebAccess,
            SteamID steamId)
        {
            var regex = new Regex("var g_rgAppContextData = (.*?);");
            Match matched = null;

            for (var i = 0; i < 3; i++)
            {
                try
                {
                    var inventoryPageResponse = await steamWebAccess.FetchString(
                        new SteamWebAccessRequest(
                            string.Format(UserInventoryUrl, steamId.ConvertToUInt64()),
                            SteamWebAccessRequestMethod.Get,
                            null
                        )
                    ).ConfigureAwait(false);

                    if (!string.IsNullOrWhiteSpace(inventoryPageResponse))
                    {
                        matched = regex.Match(inventoryPageResponse);

                        if (matched.Success)
                        {
                            break;
                        }
                    }
                }
                catch (WebException)
                {
                    continue;
                }

                await Task.Delay(TimeSpan.FromSeconds(0.3)).ConfigureAwait(false);
            }

            if (matched?.Success != true ||
                matched.Groups.Count <= 1 ||
                !matched.Groups[1].Success ||
                string.IsNullOrWhiteSpace(matched.Groups[1].Value))
            {
                throw new UserInventoryFetchOverviewException(steamId);
            }

            try
            {
                var overviewResult =
                    JsonConvert.DeserializeObject<Dictionary<long, InventoryApp>>(matched.Groups[1].Value);

                return overviewResult?.Values.Select(app => app.ToUserInventoryApp()).ToArray();
            }
            catch (Exception e)
            {
                throw new UserInventoryFetchOverviewException(steamId, e);
            }
        }


        // ReSharper disable once TooManyArguments
        // ReSharper disable once MethodTooLong
        // ReSharper disable once ExcessiveIndentation
        private static async Task<UserAppInventory> GetUserInventory(
            SteamWebAccess steamWebAccess,
            SteamID userSteamId,
            long appId,
            long contextId)
        {
            var startAssetId = 0L;
            var assets = new List<UserInventoryAsset>();
            var assetDescriptions = new List<UserInventoryAssetDescription>();

            do
            {
                var retrySuccess = false;

                for (var retry = 0; retry < 3; retry++)
                {
                    try
                    {
                        var response = await steamWebAccess.FetchObject<InventoryResponseV2>(
                            new SteamWebAccessRequest(
                                string.Format(UserInventoryV2Url, userSteamId.ConvertToUInt64(),
                                    appId,
                                    contextId),
                                SteamWebAccessRequestMethod.Get,
                                startAssetId > 0
                                    ? QueryStringBuilder.FromDynamic(new
                                    {
                                        l = "english",
                                        count = 2000,
                                        start_assetid = startAssetId
                                    })
                                    : QueryStringBuilder.FromDynamic(new
                                    {
                                        l = "english",
                                        count = 2000
                                    })
                            )
                            {
                                Referer = string.Format(UserInventoryUrl,
                                    userSteamId.ConvertToUInt64())
                            }
                        ).ConfigureAwait(false);

                        if (response?.Success == true)
                        {
                            retrySuccess = true;

                            foreach (var inventoryAsset in response.Assets)
                            {
                                var steamInventoryAsset = inventoryAsset.ToSteamInventoryAsset();

                                if (!assets.Contains(steamInventoryAsset))
                                {
                                    assets.Add(steamInventoryAsset);
                                }
                            }

                            foreach (var itemDescription in response.Descriptions)
                            {
                                var steamAssetDescription = itemDescription.ToSteamAssetDescription();

                                if (!assetDescriptions.Contains(steamAssetDescription))
                                {
                                    assetDescriptions.Add(steamAssetDescription);
                                }
                            }

                            if (!response.MoreItems || response.LastAssetId == null)
                            {
                                return new UserAppInventory(assets.ToArray(), assetDescriptions.ToArray(), null);
                            }

                            startAssetId = response.LastAssetId.Value;

                            break;
                        }
                    }
                    catch (WebException)
                    {
                        // ignored
                    }
                }

                if (!retrySuccess)
                {
                    throw new UserInventoryFetchAssetsException(appId, contextId, userSteamId);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(300)).ConfigureAwait(false);
            } while (true);
        }

        // ReSharper disable once ExcessiveIndentation
        // ReSharper disable once TooManyArguments
        private static async Task<UserAppInventory> GetUserInventoryAlternate(
            SteamWebAccess steamWebAccess,
            SteamID steamId,
            long appId,
            long contextId)
        {
            var startPosition = 0;
            var assets = new List<UserInventoryAsset>();
            var assetDescriptions = new List<UserInventoryAssetDescription>();
            var appInfos = new List<UserInventoryApp>();

            do
            {
                var retrySuccess = false;

                for (var retry = 0; retry < 3; retry++)
                {
                    try
                    {
                        var response = await steamWebAccess.FetchObject<InventoryResponseV1>(
                            new SteamWebAccessRequest(
                                string.Format(UserInventoryV1Url, steamId.ConvertToUInt64(), appId,
                                    contextId),
                                SteamWebAccessRequestMethod.Get,
                                startPosition > 0
                                    ? QueryStringBuilder.FromDynamic(new
                                    {
                                        start = startPosition
                                    })
                                    : null
                            )
                            {
                                Referer = TradeOfferManager.TradeOfferNewUrl
                            }
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

                            break;
                        }
                    }
                    catch (WebException)
                    {
                        // ignored
                    }
                }

                if (!retrySuccess)
                {
                    throw new UserInventoryFetchAssetsException(appId, contextId, steamId);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(300)).ConfigureAwait(false);
            } while (true);
        }

        public void ClearCache()
        {
            lock (_inventoryCache)
            {
                _inventoryCache.Clear();
            }
        }

        public async Task<UserInventoryAssetDescription> GetAssetDescription(UserInventoryAsset asset)
        {
            UserInventoryApp inventoryApp;
            UserInventoryAppContext appContext;

            lock (_inventoryApps)
            {
                inventoryApp = _inventoryApps.FirstOrDefault(app => app.AppId == asset.AppId);

                if (inventoryApp == null)
                {
                    inventoryApp = new UserInventoryApp(asset.AppId);
                    _inventoryApps.Add(inventoryApp);
                }

                appContext = inventoryApp.Contexts.FirstOrDefault(context => context.ContextId == asset.ContextId);

                if (appContext == null)
                {
                    appContext = new UserInventoryAppContext(asset.ContextId);
                    inventoryApp.Update(
                        new UserInventoryApp(inventoryApp.AppId, null, null, new[] {appContext}, null, null, null)
                    );
                }
            }

            lock (_inventoryCache)
            {
                if (!_inventoryCache.ContainsKey(inventoryApp.AppId))
                {
                    _inventoryCache.Add(inventoryApp.AppId, new Dictionary<long, UserAppInventory>());
                }
            }

            await FillAppContextCache(inventoryApp, appContext).ConfigureAwait(false);

            lock (_inventoryCache)
            {
                return _inventoryCache[inventoryApp.AppId]
                    .Where(pair => pair.Key == appContext.ContextId)
                    .SelectMany(pair => pair.Value.AssetDescriptions)
                    .FirstOrDefault(description => description.DoesDescribe(asset));
            }
        }

        public async Task<UserInventoryAsset[]> GetAssets()
        {
            UserInventoryApp[] inventoryAppsCopy;

            lock (_inventoryApps)
            {
                inventoryAppsCopy = _inventoryApps.ToArray();
            }

            foreach (var inventoryApp in inventoryAppsCopy)
            {
                lock (_inventoryCache)
                {
                    if (!_inventoryCache.ContainsKey(inventoryApp.AppId))
                    {
                        _inventoryCache.Add(inventoryApp.AppId, new Dictionary<long, UserAppInventory>());
                    }
                }

                foreach (var appContext in inventoryApp.Contexts)
                {
                    await FillAppContextCache(inventoryApp, appContext).ConfigureAwait(false);
                }
            }

            lock (_inventoryCache)
            {
                return _inventoryCache
                    .SelectMany(pair => pair.Value)
                    .SelectMany(pair => pair.Value.Assets)
                    .ToArray();
            }
        }

        public async Task<UserInventoryAsset[]> GetAssets(long appId)
        {
            UserInventoryApp inventoryApp;

            lock (_inventoryApps)
            {
                inventoryApp = _inventoryApps.FirstOrDefault(app => app.AppId == appId);

                if (inventoryApp == null)
                {
                    inventoryApp = new UserInventoryApp(appId);
                    _inventoryApps.Add(inventoryApp);
                }
            }

            lock (_inventoryCache)
            {
                if (!_inventoryCache.ContainsKey(inventoryApp.AppId))
                {
                    _inventoryCache.Add(inventoryApp.AppId, new Dictionary<long, UserAppInventory>());
                }
            }

            foreach (var appContext in inventoryApp.Contexts)
            {
                await FillAppContextCache(inventoryApp, appContext).ConfigureAwait(false);
            }

            lock (_inventoryCache)
            {
                return _inventoryCache[inventoryApp.AppId]
                    .SelectMany(pair => pair.Value.Assets)
                    .ToArray();
            }
        }

        public async Task<UserInventoryAsset[]> GetAssets(long appId, long contextId)
        {
            UserInventoryApp inventoryApp;
            UserInventoryAppContext appContext;

            lock (_inventoryApps)
            {
                inventoryApp = _inventoryApps.FirstOrDefault(app => app.AppId == appId);

                if (inventoryApp == null)
                {
                    inventoryApp = new UserInventoryApp(appId);
                    _inventoryApps.Add(inventoryApp);
                }

                appContext = inventoryApp.Contexts.FirstOrDefault(context => context.ContextId == contextId);

                if (appContext == null)
                {
                    appContext = new UserInventoryAppContext(contextId);
                    inventoryApp.Update(new UserInventoryApp(inventoryApp.AppId, null, null, new[] {appContext}, null,
                        null, null));
                }
            }

            lock (_inventoryCache)
            {
                if (!_inventoryCache.ContainsKey(inventoryApp.AppId))
                {
                    _inventoryCache.Add(inventoryApp.AppId, new Dictionary<long, UserAppInventory>());
                }
            }

            await FillAppContextCache(inventoryApp, appContext).ConfigureAwait(false);

            lock (_inventoryCache)
            {
                return _inventoryCache[inventoryApp.AppId]
                    .Where(pair => pair.Key == appContext.ContextId)
                    .SelectMany(pair => pair.Value.Assets)
                    .ToArray();
            }
        }

        // ReSharper disable once MethodTooLong
        // ReSharper disable once ExcessiveIndentation
        private async Task FillAppContextCache(UserInventoryApp inventoryApp, UserInventoryAppContext appContext)
        {
            await _lockObject.WaitAsync().ConfigureAwait(false);

            try
            {
                bool doesContainsContext;

                lock (_inventoryCache)
                {
                    doesContainsContext = _inventoryCache[inventoryApp.AppId].ContainsKey(appContext.ContextId);
                }

                if (!doesContainsContext)
                {
                    try
                    {
                        var contextInventory =
                            await _getInventoryDelegate(_steamWebAccess, SteamId, inventoryApp.AppId,
                                    appContext.ContextId)
                                .ConfigureAwait(false);

                        if (contextInventory != null)
                        {
                            lock (_inventoryCache)
                            {
                                _inventoryCache[inventoryApp.AppId].Add(appContext.ContextId, contextInventory);
                            }

                            foreach (var userInventoryApp in contextInventory.ExtraAppInformation)
                            {
                                lock (_inventoryApps)
                                {
                                    var oldInventoryApp =
                                        _inventoryApps.FirstOrDefault(app => app.AppId == userInventoryApp.AppId);

                                    if (oldInventoryApp != null)
                                    {
                                        oldInventoryApp.Update(userInventoryApp);
                                    }
                                    else
                                    {
                                        _inventoryApps.Add(userInventoryApp);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw new UserInventoryFetchAssetsException(inventoryApp.AppId, appContext.ContextId, SteamId,
                            e);
                    }
                }
            }
            finally
            {
                _lockObject.Release();
            }
        }

        internal delegate Task<UserAppInventory> InventoryRetrieveDelegate(
            SteamWebAccess steamWebAccess,
            SteamID userSteamId,
            long appId,
            long contextId);
    }
}