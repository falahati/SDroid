using System;
using System.Collections.Generic;
using System.Linq;
using SDroid.SteamTrade.InternalModels.EconomyServiceAPI;
using SDroid.SteamTrade.InternalModels.EconomyServiceAPI.Constants;
using SDroid.SteamTrade.Models.TradeOffer;
using SDroid.SteamTrade.Models.UserInventory;
using SteamKit2;

namespace SDroid.SteamTrade
{
    public class TradeOffer
    {
        private readonly UserInventoryAssetDescription[] _descriptions;

        internal TradeOffer(InternalModels.EconomyServiceAPI.TradeOffer offer, AssetDescription[] descriptions = null)
        {
            if (offer.TradeOfferId == null)
            {
                throw new ArgumentException("TradeOfferId is missing or invalid.");
            }

            _descriptions = descriptions?.Select(description => description.ToSteamAssetDescription()).Distinct()
                .ToArray();

            var ourAssets = new List<TradeOfferAsset>();
            var theirAssets = new List<TradeOfferAsset>();

            if (offer.ItemsToGive != null)
            {
                foreach (var asset in offer.ItemsToGive)
                {
                    // TODO: for currency items we need to check descriptions for currency bool and use the appropriate method
                    if (asset.IsMissing)
                    {
                        HasMissing = true;
                    }

                    ourAssets.Add(asset.ToTradeOfferAsset());
                }
            }

            if (offer.ItemsToReceive != null)
            {
                foreach (var asset in offer.ItemsToReceive)
                {
                    // TODO: for currency items we need to check descriptions for currency bool and use the appropriate method
                    if (asset.IsMissing)
                    {
                        HasMissing = true;
                    }

                    theirAssets.Add(asset.ToTradeOfferAsset());
                }
            }

            // Assume public individual
            TradeOfferId = offer.TradeOfferId.Value;
            IsOurOffer = offer.IsOurOffer;
            ExpirationTime = offer.ExpirationTime <= 0
                ? (DateTime?) null
                : TradeOfferManager.Epoch.AddSeconds(offer.ExpirationTime);
            TimeCreated = offer.TimeCreated <= 0
                ? (DateTime?) null
                : TradeOfferManager.Epoch.AddSeconds(offer.TimeCreated);
            TimeUpdated = offer.TimeUpdated <= 0
                ? (DateTime?) null
                : TradeOfferManager.Epoch.AddSeconds(offer.TimeUpdated);
            Message = offer.Message;
            OurAssets = ourAssets.ToArray();
            TheirAssets = theirAssets.ToArray();
            PartnerSteamId = new SteamID(Convert.ToUInt32(offer.AccountIdOther), EUniverse.Public,
                EAccountType.Individual);
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

        public DateTime? ExpirationTime { get; }

        public bool HasMissing { get; }

        public bool IsFirstOffer
        {
            get => TimeCreated == TimeUpdated;
        }

        public bool IsOurOffer { get; }

        public string Message { get; }

        public TradeOfferAsset[] OurAssets { get; }

        public SteamID PartnerSteamId { get; }

        public TradeOfferStatus Status { get; }

        public TradeOfferAsset[] TheirAssets { get; }

        public DateTime? TimeCreated { get; }

        public DateTime? TimeUpdated { get; }

        public long? TradeId { get; }

        public long TradeOfferId { get; }

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