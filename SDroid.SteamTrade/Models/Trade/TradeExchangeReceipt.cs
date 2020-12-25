using System;
using System.Collections.Generic;
using System.Linq;
using SDroid.SteamTrade.InternalModels.EconomyServiceAPI;
using SDroid.SteamTrade.InternalModels.EconomyServiceAPI.Constants;
using SDroid.SteamTrade.Models.TradeOffer;
using SDroid.SteamTrade.Models.UserInventory;
using SteamKit2;

namespace SDroid.SteamTrade.Models.Trade
{
    public class TradeExchangeReceipt
    {
        private readonly UserInventoryAssetDescription[] _descriptions;

        internal TradeExchangeReceipt(TradeExchangeStatus offer, TradeExchangeStatusAssetDescription[] descriptions = null)
        {
            _descriptions = descriptions?.Select(description => description.ToSteamAssetDescription()).Distinct().ToArray();

            var ourAssets = new List<TradeExchangeAsset>();
            var theirAssets = new List<TradeExchangeAsset>();

            if (offer.ItemsGiven != null)
            {
                foreach (var asset in offer.ItemsGiven)
                {
                    // TODO: for currency items we need to check descriptions for currency bool and use the appropriate method
                    ourAssets.Add(asset.ToTradeExchangeAsset());
                }
            }

            if (offer.ItemsReceived != null)
            {
                foreach (var asset in offer.ItemsReceived)
                {
                    // TODO: for currency items we need to check descriptions for currency bool and use the appropriate method
                    theirAssets.Add(asset.ToTradeExchangeAsset());
                }
            }

            TimeCreated = offer.TimeCreated <= 0
                ? (DateTime?) null
                : TradeOfferManager.Epoch.AddSeconds(offer.TimeCreated);
            OurAssets = ourAssets.ToArray();
            TheirAssets = theirAssets.ToArray();
            PartnerSteamId = new SteamID(offer.PartnerSteamId);
            TradeId = offer.TradeId;

            switch (offer.State)
            {
                case EconomyTradeOfferState.Active:
                    Status = TradeOfferStatus.Active;

                    break;
                case EconomyTradeOfferState.Accepted:
                    Status = TradeOfferStatus.Accepted;

                    break;
                case EconomyTradeOfferState.Countered:
                    Status = TradeOfferStatus.Countered;

                    break;
                case EconomyTradeOfferState.Expired:
                    Status = TradeOfferStatus.Expired;

                    break;
                case EconomyTradeOfferState.Canceled:
                    Status = TradeOfferStatus.Canceled;

                    break;
                case EconomyTradeOfferState.Declined:
                    Status = TradeOfferStatus.Declined;

                    break;
                case EconomyTradeOfferState.InvalidItems:
                    Status = TradeOfferStatus.InvalidItems;

                    break;
                case EconomyTradeOfferState.NeedsConfirmation:
                    Status = TradeOfferStatus.NeedsConfirmation;

                    break;
                case EconomyTradeOfferState.CanceledBySecondFactor:
                    Status = TradeOfferStatus.CanceledBySecondFactor;

                    break;
                case EconomyTradeOfferState.InEscrow:
                    Status = TradeOfferStatus.InEscrow;

                    break;
                default:
                    Status = TradeOfferStatus.Invalid;

                    break;
            }
        }

        public TradeExchangeAsset[] OurAssets { get; }

        public SteamID PartnerSteamId { get; }

        public TradeOfferStatus Status { get; }

        public TradeExchangeAsset[] TheirAssets { get; }

        public DateTime? TimeCreated { get; }

        public long TradeId { get; }

        public UserInventoryAssetDescription GetAssetDescription(UserInventoryAsset asset)
        {
            if (_descriptions == null || asset == null)
            {
                return null;
            }

            return _descriptions.FirstOrDefault(description => description.DoesDescribe(asset));
        }
    }
}