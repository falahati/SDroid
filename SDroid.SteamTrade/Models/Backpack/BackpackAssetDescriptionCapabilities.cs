using SDroid.SteamTrade.InternalModels.EconomyItemsAPI;

namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAssetDescriptionCapabilities
    {
        internal BackpackAssetDescriptionCapabilities(SchemaItemCapabilities capabilities)
        {
            CanBeRestored = capabilities?.CanBeRestored ?? false;

            CanCardUpgrade = capabilities?.CanCardUpgrade ?? false;

            CanConsume = capabilities?.CanConsume ?? false;

            CanCraftCount = capabilities?.CanCraftCount ?? false;

            CanCraftMark = capabilities?.CanCraftMark ?? false;

            CanGiftWrap = capabilities?.CanGiftWrap ?? false;

            CanKillStreakify = capabilities?.CanKillStreakify ?? false;

            CanStrangify = capabilities?.CanStrangify ?? false;

            IsNameable = capabilities?.IsNameable ?? false;

            IsPaintable = capabilities?.IsPaintable ?? false;

            IsStrangeParts = capabilities?.IsStrangeParts ?? false;

            IsUsable = capabilities?.IsUsable ?? false;

            IsUsableGc = capabilities?.IsUsableGc ?? false;

            IsUsableOutOfGame = capabilities?.IsUsableOutOfGame ?? false;
        }

        public bool CanBeRestored { get; }

        public bool CanCardUpgrade { get; }

        public bool CanConsume { get; }

        public bool CanCraftCount { get; }

        public bool CanCraftMark { get; }

        public bool CanGiftWrap { get; }

        public bool CanKillStreakify { get; }

        public bool CanStrangify { get; }

        public bool IsNameable { get; }

        public bool IsPaintable { get; }

        public bool IsStrangeParts { get; }

        public bool IsUsable { get; }

        public bool IsUsableGc { get; }

        public bool IsUsableOutOfGame { get; }
    }
}