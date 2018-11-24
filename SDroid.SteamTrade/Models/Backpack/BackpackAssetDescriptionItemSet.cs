using System.Collections.Generic;
using System.Linq;
using SDroid.SteamTrade.InternalModels.EconomyItemsAPI;

namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAssetDescriptionItemSet
    {
        internal BackpackAssetDescriptionItemSet(
            SchemaOverviewItemSet itemSet,
            SchemaOverviewAttribute[] attributeOverviews)
        {
            Key = itemSet?.ItemSet;
            Name = itemSet?.Name;
            StoreBundleName = itemSet?.StoreBundleName;
            Items = itemSet?.Items;

            var attributes = new List<BackpackAssetDescriptionAttribute>();

            foreach (var itemSetAttribute in itemSet?.Attributes ?? new SchemaAttribute[0])
            {
                var attributeOverview = attributeOverviews?.FirstOrDefault(overviewAttribute =>
                    overviewAttribute.AttributeClass == itemSetAttribute.AttributeClass);

                if (attributeOverview != null)
                {
                    attributes.Add(new BackpackAssetDescriptionAttribute(attributeOverview, itemSetAttribute));
                }
            }

            Attributes = attributes.ToArray();
        }

        public BackpackAssetDescriptionAttribute[] Attributes { get; }
        public string[] Items { get; }
        public string Key { get; }
        public string Name { get; }
        public string StoreBundleName { get; }
    }
}