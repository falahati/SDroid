using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SDroid.SteamTrade.EventArguments;
using SDroid.SteamTrade.Exceptions;
using SDroid.SteamTrade.InternalModels.EconomyServiceAPI;
using SDroid.SteamTrade.InternalModels.TradeJson;
using SDroid.SteamTrade.Models.Trade;
using SDroid.SteamTrade.Models.TradeOffer;
using SDroid.SteamWeb;
using SDroid.SteamWeb.Models;
using SteamKit2;

namespace SDroid.SteamTrade
{
    // ReSharper disable once HollowTypeName
    public class TradeManager : IDisposable
    {
        private const string TradeReceiptUrl = Trade.TradeUrl + "/receipt";
        public event EventHandler<TradeCreatedEventArgs> TradeCreated;
        private readonly SemaphoreSlim _lockObject = new SemaphoreSlim(1, 1);
        private readonly SteamWebAccess _steamWebAccess;
        private readonly SteamWebAPI _steamWebAPI;
        private readonly List<Trade> _trades = new List<Trade>();
        private TradeOptions _tradeOptions = TradeOptions.Default;

        public TradeManager(SteamWebAPI steamWebAPI, SteamWebAccess steamWebAccess, TradeOptions options = null)
        {
            _steamWebAPI = steamWebAPI ?? throw new ArgumentNullException(nameof(steamWebAPI));
            _steamWebAccess = steamWebAccess ?? throw new ArgumentNullException(nameof(steamWebAccess));
            TradeOptions = options;
        }

        public TradeOptions TradeOptions
        {
            get => _tradeOptions;
            set => _tradeOptions = value ?? TradeOptions.Default;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _lockObject?.Dispose();
        }

        internal static async Task<TradeReceipt> GetReceipt(
            OperationRetryHelper retryHelper,
            SteamWebAccess steamWebAccess,
            long tradeId)
        {
            var response = await retryHelper.RetryOperationAsync(
                () => steamWebAccess.FetchString(
                    new SteamWebAccessRequest(
                        string.Format(TradeReceiptUrl, tradeId),
                        SteamWebAccessRequestMethod.Get,
                        new QueryStringBuilder()
                    )
                    {
                        Referer = string.Format(Trade.TradeUrl, tradeId)
                    }
                ),
                shouldThrowExceptionOnTotalFailure: false
            ).ConfigureAwait(false);

            var errorMatched = Regex.Match(response, "<div id=\"error_msg\">\\s*([^<]+)\\s*<\\/div>");

            if (errorMatched.Success)
            {
                if (errorMatched.Success &&
                    errorMatched.Groups.Count > 1 &&
                    errorMatched.Groups[1].Success &&
                    !string.IsNullOrWhiteSpace(errorMatched.Groups[1].Value)
                )
                {
                    throw new TradeException(errorMatched.Groups[1].Value.Trim());
                }

                throw new TradeException("Failed to retrieve trade receipt.");
            }

            var assets = new List<TradeReceiptAsset>();
            var matchedAssets = new Regex("oItem = (.*?)\r\n").Matches(response);

            foreach (Match matchedAsset in matchedAssets)
            {
                if (matchedAsset.Success &&
                    matchedAsset.Groups.Count > 1 &&
                    matchedAsset.Groups[1].Success &&
                    !string.IsNullOrWhiteSpace(matchedAsset.Groups[1].Value)
                )
                {
                    try
                    {
                        var asset = JsonConvert.DeserializeObject<TradeReceiptAsset>(
                            matchedAsset.Groups[1].Value.Trim().Trim(';').Trim()
                        );

                        if (asset != null)
                        {
                            assets.Add(asset);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            if (assets.Count == 0)
            {
                throw new TradeException("Failed to retrieve trade receipt.");
            }

            return new TradeReceipt(assets.ToArray());
        }

        internal static async Task<TradeExchangeReceipt> GetExchangeReceipt(
            OperationRetryHelper retryHelper,
            SteamWebAPI steamWebAPI,
            long tradeId
        )
        {
            var response = await retryHelper.RetryOperationAsync(
                () => steamWebAPI.RequestObject<SteamWebAPIResponse<GetTradeStatusResponse>>(
                    "IEconService",
                    SteamWebAccessRequestMethod.Get,
                    "GetTradeStatus",
                    "v1",
                    new
                    {
                        tradeid = tradeId,
                        get_descriptions = 1,
                    }
                ),
                shouldThrowExceptionOnTotalFailure: false
            ).ConfigureAwait(false);

            if (response.Response == null || response.Response.Trades.Count == 0)
            {
                throw new TradeException("Failed to retrieve trade receipt.");
            }

            return new TradeExchangeReceipt(response.Response.Trades[0], response.Response.Descriptions.ToArray());
        }

        public async Task<Trade> CreateTrade(SteamID tradePartner)
        {
            if (!await ValidateAccess(tradePartner).ConfigureAwait(false))
            {
                throw new TradeException("Can not create a trade. Ask for an invite or send a new one.");
            }

            await _lockObject.WaitAsync().ConfigureAwait(false);

            try
            {
                var trade = new Trade(tradePartner, _steamWebAccess, TradeOptions);
                _trades.Add(trade);

                OnTradeCreated(new TradeCreatedEventArgs(tradePartner, trade));

                return trade;
            }
            finally
            {
                _lockObject.Release();
            }
        }

        public async Task<Trade[]> GetActiveTrades()
        {
            await _lockObject.WaitAsync().ConfigureAwait(false);

            try
            {
                await ValidateTrades().ConfigureAwait(false);

                return _trades.Where(trade => trade.Status == TradeStatus.Active).ToArray();
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                _lockObject.Release();
            }

            return new Trade[0];
        }

        public Task<TradeReceipt> GetReceipt(Trade trade)
        {
            if (trade.Status != TradeStatus.Completed || !(trade.TradeId > 0))
            {
                throw new InvalidOperationException("Can't get a receipt for a trade that is not yet completed.");
            }

            return GetReceipt(TradeOptions, _steamWebAccess, trade.TradeId.Value);
        }

        public Task<TradeReceipt> GetReceipt(long tradeId)
        {
            if (!(tradeId > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(tradeId));
            }

            return GetReceipt(TradeOptions, _steamWebAccess, tradeId);
        }


        public async Task<Trade[]> GetWaitingConfirmationTrades()
        {
            await _lockObject.WaitAsync().ConfigureAwait(false);

            try
            {
                await ValidateTrades().ConfigureAwait(false);

                return _trades.Where(trade => trade.Status == TradeStatus.CompletedWaitingForConfirmation).ToArray();
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                _lockObject.Release();
            }

            return new Trade[0];
        }

        private void OnTradeCreated(TradeCreatedEventArgs eventArgs)
        {
            TradeCreated?.Invoke(this, eventArgs);
        }

        private async Task<bool> ValidateAccess(SteamID partnerSteamId)
        {
            try
            {
                var serverResponse = await _steamWebAccess.FetchString(
                    new SteamWebAccessRequest(
                        string.Format(Trade.TradeUrl, partnerSteamId.ConvertToUInt64()),
                        SteamWebAccessRequestMethod.Get,
                        null
                    )
                    {
                        Referer = SteamWebAccess.CommunityBaseUrl
                    }
                ).ConfigureAwait(false);

                return serverResponse.ToLower().Contains(partnerSteamId.ConvertToUInt64().ToString());
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        // ReSharper disable once ExcessiveIndentation
        private async Task ValidateTrades()
        {
            foreach (var trade in _trades.ToArray())
            {
                if (trade.Status == TradeStatus.Canceled || trade.Status == TradeStatus.Completed)
                {
                    _trades.Remove(trade);
                }
                else if (trade.Status == TradeStatus.CompletedWaitingForConfirmation)
                {
                    if (trade.TradeId != null)
                    {
                        try
                        {
                            await Task.Delay(TradeOptions.RequestDelay).ConfigureAwait(false);

                            var tradeOffer = await TradeOfferManager
                                .GetTradeOffer(_steamWebAPI, trade.TradeId.Value, _tradeOptions)
                                .ConfigureAwait(false);

                            if (tradeOffer != null &&
                                tradeOffer.Status != TradeOfferStatus.NeedsConfirmation &&
                                tradeOffer.Status != TradeOfferStatus.Invalid)
                            {
                                _trades.Remove(trade);
                            }
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                    else
                    {
                        _trades.Remove(trade);
                    }
                }
            }
        }
    }
}