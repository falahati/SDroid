using System.Linq;
using SDroid.SteamTrade.InternalModels.TradeJson;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.Models.Trade
{
    public class TradeReceipt
    {
        private readonly TradeReceiptAsset[] _receiptAssets;

        internal TradeReceipt(TradeReceiptAsset[] receiptAssets)
        {
            _receiptAssets = receiptAssets;
        }

        public UserInventoryAsset[] Assets
        {
            get { return _receiptAssets.Select(asset => asset.ToSteamInventoryAsset()).ToArray(); }
        }

        public UserInventoryAssetDescription GetAssetDescription(UserInventoryAsset asset)
        {
            return _receiptAssets.FirstOrDefault(receiptAsset => receiptAsset.ToSteamInventoryAsset().Equals(asset))
                ?.ToSteamAssetDescription();
        }
    }
}