using System.Collections.Generic;
using SDroid.SteamTrade.InternalModels.EconomyItemsAPI;

namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAssetDescriptionStyle
    {
        internal BackpackAssetDescriptionStyle(SchemaItemStyle style)
        {
            Name = style?.Name;
            AdditionalHiddenBodyGroups = style?.AdditionalHiddenBodyGroups ?? new Dictionary<string, bool>();
        }

        public Dictionary<string, bool> AdditionalHiddenBodyGroups { get; }
        public string Name { get; }
    }
}