using SDroid.SteamTrade.InternalModels.EconomyItemsAPI;
using SteamKit2;

namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAssetDescriptionAttributeAccount
    {
        internal BackpackAssetDescriptionAttributeAccount(ItemAttributeAccountInfo accountInfo)
        {
            SteamId = accountInfo != null ? new SteamID(accountInfo.CommunitySteamId) : null;
            PersonaName = accountInfo?.PersonaName;
        }

        public string PersonaName { get; }
        public SteamID SteamId { get; }
    }
}