using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SDroid.SteamTrade.Exceptions;
using SDroid.SteamTrade.Helpers;
using SDroid.SteamTrade.InternalModels.EconomyItemsAPI;
using SDroid.SteamTrade.InternalModels.EconomyItemsAPI.Constants;
using SDroid.SteamTrade.Models.Backpack;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Models;
using SteamKit2;

namespace SDroid.SteamTrade
{
    public class Backpack : IAssetInventory
    {
        private const int BackpackContextId = 2;
        private static readonly SemaphoreSlim LockObject = new SemaphoreSlim(1, 1);
        private static readonly Dictionary<long, SchemaItem[]> SchemaItems = new Dictionary<long, SchemaItem[]>();

        private static readonly Dictionary<long, GetSchemaOverviewResult> SchemaOverviewResults =
            new Dictionary<long, GetSchemaOverviewResult>();

        private readonly GetPlayerItemsResult _playerItemsResult;
        private readonly SteamWebAPI _steamWebAPI;

        private Backpack(
            long appId,
            SteamID steamId,
            GetPlayerItemsResult getPlayerItemsResult,
            SteamWebAPI steamWebAPI)
        {
            AppId = appId;
            SteamId = steamId;
            _playerItemsResult = getPlayerItemsResult ?? throw new ArgumentNullException(nameof(getPlayerItemsResult));
            _steamWebAPI = steamWebAPI ?? throw new ArgumentNullException(nameof(steamWebAPI));
        }

        public long AppId { get; }

        public int BackpackSlots
        {
            get => _playerItemsResult?.BackpackSlots ?? 0;
        }

        /// <inheritdoc />
        Task<Asset[]> IAssetInventory.GetAssets()
        {
            return Task.FromResult(GetAssets().Cast<Asset>().ToArray());
        }

        /// <inheritdoc />
        public SteamID SteamId { get; }

        public static async Task<Backpack> GetBackpack(SteamWebAPI steamWebAPI, SteamID steamId, long appId)
        {
            SteamWebAPIResultResponse<GetPlayerItemsResult> serverResponse;

            try
            {
                serverResponse = await OperationRetryHelper.Default.RetryOperationAsync(() =>
                        steamWebAPI.RequestObject<SteamWebAPIResultResponse<GetPlayerItemsResult>>(
                            string.Format("IEconItems_{0}", appId),
                            SteamWebAccessRequestMethod.Get,
                            "GetPlayerItems",
                            "v0001",
                            new
                            {
                                steamid = steamId.ConvertToUInt64().ToString()
                            }
                        ),
                    r => Task.FromResult(r?.Result?.Items != null)
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new BackpackFetchAssetsException(appId, steamId, null, e);
            }

            if (serverResponse?.Result?.Items == null)
            {
                throw new BackpackFetchAssetsException(appId, steamId);
            }

            return new Backpack(
                appId,
                steamId,
                serverResponse.Result,
                steamWebAPI
            );
        }

        private static async Task<SchemaItem[]> FetchSchemaItems(SteamWebAPI steamWebAPI, long appId)
        {
            var items = new List<SchemaItem>();
            var nextAssetId = 0;

            do
            {
                var nextAssetIdCopy = nextAssetId;
                var serverResponse = await OperationRetryHelper.Default.RetryOperationAsync(() =>
                        steamWebAPI.RequestObject<SteamWebAPIResultResponse<GetSchemaItemsResult>>(
                            string.Format("IEconItems_{0}", appId),
                            SteamWebAccessRequestMethod.Get,
                            "GetSchemaItems",
                            "v1",
                            nextAssetIdCopy > 0
                                ? new
                                {
                                    start = nextAssetIdCopy
                                }
                                : null
                        ),
                    r => Task.FromResult(r?.Result?.Items?.Length > 0),
                    false
                ).ConfigureAwait(false);

                if (serverResponse?.Result?.Status != GetSchemaStatus.Success &&
                    !string.IsNullOrWhiteSpace(serverResponse?.Result?.WebApiErrorMessage))
                {
                    throw new BackpackFetchSchemaException(appId, serverResponse.Result.WebApiErrorMessage);
                }

                if (!(serverResponse?.Result?.Items?.Length > 0))
                {
                    throw new BackpackFetchSchemaException(appId);
                }

                items.AddRange(serverResponse.Result.Items);
                nextAssetId = serverResponse.Result.Next;

                if (nextAssetId <= items.Count)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(0.3)).ConfigureAwait(false);
            } while (true);

            return items.ToArray();
        }

        private static async Task<GetSchemaOverviewResult> FetchSchemaOverview(SteamWebAPI steamWebAPI, long appId)
        {
            SteamWebAPIResultResponse<GetSchemaOverviewResult> serverResponse;

            try
            {
                serverResponse = await OperationRetryHelper.Default.RetryOperationAsync(() =>
                        steamWebAPI.RequestObject<SteamWebAPIResultResponse<GetSchemaOverviewResult>>(
                            string.Format("IEconItems_{0}", appId),
                            SteamWebAccessRequestMethod.Get,
                            "GetSchemaOverview"
                        ),
                    r => Task.FromResult(r?.Result?.Status == GetSchemaOverviewStatus.Success)
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new BackpackFetchSchemaException(appId,
                    $"Failed to retrieve schema overview for appId {appId}.", e);
            }

            if (serverResponse?.Result?.Status != GetSchemaOverviewStatus.Success)
            {
                throw new BackpackFetchSchemaException(appId,
                    $"Failed to retrieve schema overview for appId {appId}.");
            }

            return serverResponse.Result;
        }

        private static async Task<bool> FillSchemaItems(SteamWebAPI steamWebAPI, long appId)
        {
            await LockObject.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!SchemaItems.ContainsKey(appId))
                {
                    var schemaItemsResult = await DiskCache.Default.EnsureAsync(
                        "SchemaItems_" + appId,
                        TimeSpan.FromDays(1),
                        () => FetchSchemaItems(steamWebAPI, appId)
                    ).ConfigureAwait(false);

                    SchemaItems.Add(appId, schemaItemsResult);
                }

                return SchemaItems.ContainsKey(appId);
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                LockObject.Release();
            }

            return false;
        }

        private static async Task<bool> FillSchemaOverview(SteamWebAPI steamWebAPI, long appId)
        {
            await LockObject.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!SchemaOverviewResults.ContainsKey(appId))
                {
                    var schemaOverviewResult = await DiskCache.Default.EnsureAsync(
                        "SchemaOverview_" + appId,
                        TimeSpan.FromDays(1),
                        () => FetchSchemaOverview(steamWebAPI, appId)
                    ).ConfigureAwait(false);
                    SchemaOverviewResults.Add(appId, schemaOverviewResult);
                }

                return SchemaOverviewResults.ContainsKey(appId);
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                LockObject.Release();
            }

            return false;
        }

        public async Task<BackpackAssetDescription> GetAssetDescription(Asset asset)
        {
            var playerItem = _playerItemsResult.Items.FirstOrDefault(item => item.AssetId == asset.AssetId);

            if (playerItem == null)
            {
                return null;
            }

            if (!SchemaItems.ContainsKey(AppId))
            {
                await FillSchemaItems(_steamWebAPI, AppId).ConfigureAwait(false);
            }

            if (!SchemaOverviewResults.ContainsKey(AppId))
            {
                await FillSchemaOverview(_steamWebAPI, AppId).ConfigureAwait(false);
            }

            var itemSchema = SchemaItems.ContainsKey(AppId)
                ? SchemaItems[AppId]?.FirstOrDefault(item => item.DefinitionIndex == playerItem.DefinitionIndex)
                : null;

            return new BackpackAssetDescription(playerItem, itemSchema,
                SchemaOverviewResults.ContainsKey(AppId) ? SchemaOverviewResults[AppId] : null);
        }

        public BackpackAsset[] GetAssets()
        {
            return _playerItemsResult?.Items?.Select(item =>
                           new BackpackAsset(AppId, BackpackContextId, item.AssetId, item.DefinitionIndex,
                               item.Quantity))
                       .ToArray() ??
                   new BackpackAsset[0];
        }
    }
}