using System;

namespace SDroid.SteamTrade.Models.UserInventory
{
    internal class UserAppInventory
    {
        public UserAppInventory(
            UserInventoryAsset[] assets,
            UserInventoryAssetDescription[] assetDescriptions,
            UserInventoryApp[] extraAppInformation)
        {
            Assets = assets ?? new UserInventoryAsset[0];
            AssetDescriptions = assetDescriptions ?? new UserInventoryAssetDescription[0];
            ExtraAppInformation = extraAppInformation ?? new UserInventoryApp[0];
            LastUpdate = DateTime.Now;
        }

        public UserInventoryAssetDescription[] AssetDescriptions { get; }

        public UserInventoryAsset[] Assets { get; }

        public UserInventoryApp[] ExtraAppInformation { get; }

        public DateTime LastUpdate { get; }
    }
}