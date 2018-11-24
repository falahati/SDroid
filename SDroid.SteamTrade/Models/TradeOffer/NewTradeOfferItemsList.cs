using System.Collections.Generic;
using System.Linq;
using SDroid.SteamTrade.InternalModels.TradeOfferJson;
using SDroid.SteamTrade.Models.Backpack;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.Models.TradeOffer
{
    public class NewTradeOfferItemsList
    {
        private readonly List<Asset> _ourAssets = new List<Asset>();
        private readonly List<Asset> _theirAssets = new List<Asset>();

        public NewTradeOfferItemsList()
        {
        }

        public NewTradeOfferItemsList(Asset[] ourAssets, Asset[] theirAssets)
        {
            _ourAssets.AddRange(ourAssets);
            _theirAssets.AddRange(theirAssets);
        }

        public NewTradeOfferItemsList(UserInventoryAsset[] ourAssets, UserInventoryAsset[] theirAssets)
        {
            _ourAssets.AddRange(ourAssets.Cast<Asset>().ToArray());
            _theirAssets.AddRange(theirAssets.Cast<Asset>().ToArray());
        }

        public NewTradeOfferItemsList(BackpackAsset[] ourAssets, BackpackAsset[] theirAssets)
        {
            _ourAssets.AddRange(ourAssets.Cast<Asset>().ToArray());
            _theirAssets.AddRange(theirAssets.Cast<Asset>().ToArray());
        }

        public Asset[] OurAssets
        {
            get => _ourAssets.ToArray();
        }

        public Asset[] TheirAssets
        {
            get => _theirAssets.ToArray();
        }

        public bool AddOurItem(Asset asset)
        {
            if (!_ourAssets.Contains(asset))
            {
                _ourAssets.Add(asset);

                return true;
            }

            return false;
        }

        public bool AddTheirItem(Asset asset)
        {
            if (!_theirAssets.Contains(asset))
            {
                _theirAssets.Add(asset);

                return true;
            }

            return false;
        }

        public bool RemoveOurItem(Asset asset)
        {
            if (_ourAssets.Contains(asset))
            {
                return _ourAssets.Remove(asset);
            }

            return false;
        }

        public bool RemoveTheirItem(Asset asset)
        {
            if (_theirAssets.Contains(asset))
            {
                return _theirAssets.Remove(asset);
            }

            return false;
        }

        internal TradeOfferState AsTradeOfferStatus(int version = 1)
        {
            return new TradeOfferState(OurAssets, TheirAssets, version);
        }
    }
}