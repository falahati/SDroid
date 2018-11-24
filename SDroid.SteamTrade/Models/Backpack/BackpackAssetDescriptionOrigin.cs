using SDroid.SteamTrade.InternalModels.EconomyItemsAPI;

namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAssetDescriptionOrigin
    {
        internal BackpackAssetDescriptionOrigin(SchemaOverviewOriginName originName)
        {
            Id = originName?.Origin ?? -1;
            Name = originName?.Name;
        }

        public int Id { get; }
        public string Name { get; }
    }
}