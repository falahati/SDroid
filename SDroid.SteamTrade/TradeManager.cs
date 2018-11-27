using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SDroid.SteamTrade.EventArguments;
using SDroid.SteamTrade.Exceptions;
using SDroid.SteamTrade.Models.Trade;
using SDroid.SteamTrade.Models.TradeOffer;
using SDroid.SteamWeb;
using SteamKit2;

namespace SDroid.SteamTrade
{
    // ReSharper disable once HollowTypeName
    public class TradeManager : IDisposable
    {
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