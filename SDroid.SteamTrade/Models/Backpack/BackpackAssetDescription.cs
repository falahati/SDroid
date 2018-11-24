using System.Linq;
using SDroid.SteamTrade.InternalModels.EconomyItemsAPI;

namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAssetDescription
    {
        /// <inheritdoc />
        // ReSharper disable once CyclomaticComplexity
        // ReSharper disable once FunctionComplexityOverflow
        internal BackpackAssetDescription(
            Item playerItem,
            SchemaItem schemaItem,
            GetSchemaOverviewResult schemaOverview)
        {
            // Schema information
            DefinitionIndex = schemaItem?.DefinitionIndex ?? -1;
            ImageUrl = schemaItem?.ImageUrl;
            ImageLargeUrl = schemaItem?.ImageUrlLarge;
            Name = schemaItem?.Name ?? schemaItem?.ItemName;
            Class = schemaItem?.ItemClass;
            TypeName = schemaItem?.ItemTypeName;
            CraftClass = schemaItem?.CraftClass;
            CraftMaterialType = schemaItem?.CraftMaterialType;
            HolidayRestriction = schemaItem?.HolidayRestriction;
            DropType = schemaItem?.DropType;
            Description = schemaItem?.ItemDescription;
            MainSlotName = schemaItem?.ItemSlot;
            ClassSlots = schemaItem?.PerClassLoadOutSlots?
                             .Select(pair => new BackpackAssetDescriptionClassSlot(pair.Key, pair.Value)).ToArray() ??
                         new BackpackAssetDescriptionClassSlot[0];
            UsableByClasses = schemaItem?.UsableByClasses ?? new string[0];
            Capabilities = new BackpackAssetDescriptionCapabilities(schemaItem?.Capabilities);
            ToolType = schemaItem?.Tool?.Type;

            if (!string.IsNullOrWhiteSpace(schemaItem?.ItemSet))
            {
                var itemSet = schemaOverview?.ItemSets?.FirstOrDefault(set => set.ItemSet == schemaItem.ItemSet);

                if (itemSet != null)
                {
                    ItemSet = new BackpackAssetDescriptionItemSet(itemSet, schemaOverview.Attributes);
                }
            }

            // Player item information
            AssetId = playerItem?.AssetId ?? -1;
            Level = playerItem?.Level ?? 0;
            IsTradable = !(playerItem?.IsNotTradable ?? true);
            IsCraftable = !(playerItem?.IsNotCraftable ?? true);
            EquippedSlots = playerItem?.EquippedSlot?.Select(slot =>
                                    new BackpackAssetDescriptionEquippedSlot(slot.CharacterClassId,
                                        slot.CharacterSlotId))
                                .ToArray() ??
                            new BackpackAssetDescriptionEquippedSlot[0];

            if (playerItem?.Origin != null)
            {
                var originName = schemaOverview?.OriginNames?.FirstOrDefault(name => name.Origin == playerItem.Origin);

                if (originName != null)
                {
                    Origin = new BackpackAssetDescriptionOrigin(originName);
                }
            }

            if (playerItem?.Style != null && playerItem.Style < schemaItem?.Styles?.Length)
            {
                Style = new BackpackAssetDescriptionStyle(schemaItem.Styles[playerItem.Style]);
            }

            // Shared or duplicate information
            var quality = playerItem?.Quality != null
                ? schemaOverview?.Qualities.FirstOrDefault(pair => pair.Value == playerItem.Quality)
                : (schemaItem?.ItemQuality != null
                    ? schemaOverview?.Qualities.FirstOrDefault(pair => pair.Value == schemaItem.ItemQuality)
                    : null);

            if (quality?.Key != null)
            {
                var qualityName = schemaOverview.QualityNames.FirstOrDefault(pair => pair.Key == quality.Value.Key)
                    .Value;

                if (!string.IsNullOrWhiteSpace(qualityName))
                {
                    Quality = new BackpackAssetDescriptionQuality(quality.Value.Value, quality.Value.Key, qualityName);
                }
            }

            if (schemaOverview?.Attributes != null)
            {
                var playerItemAttributes = playerItem?.Attributes.Select(attribute =>
                                                   new BackpackAssetDescriptionAttribute(
                                                       schemaOverview.Attributes.FirstOrDefault(overviewAttribute =>
                                                           overviewAttribute.DefinitionIndex ==
                                                           attribute.DefinitionIndex),
                                                       attribute)
                                               ).Where(attribute => !string.IsNullOrWhiteSpace(attribute.AttributeName))
                                               .ToArray() ??
                                           new BackpackAssetDescriptionAttribute[0];
                var schemaItemAttributes = schemaItem?.Attributes.Select(attribute =>
                                                   new BackpackAssetDescriptionAttribute(
                                                       schemaOverview.Attributes.FirstOrDefault(overviewAttribute =>
                                                           overviewAttribute.AttributeClass ==
                                                           attribute.AttributeClass),
                                                       attribute
                                                   )
                                               ).Where(attribute => !string.IsNullOrWhiteSpace(attribute.AttributeName))
                                               .ToArray() ??
                                           new BackpackAssetDescriptionAttribute[0];
                Attributes = playerItemAttributes.Concat(schemaItemAttributes)
                    .GroupBy(attribute => attribute.DefinitionIndex)
                    .Select(grouping => grouping.FirstOrDefault())
                    .ToArray();
            }
            else
            {
                Attributes = new BackpackAssetDescriptionAttribute[0];
            }
        }

        public long AssetId { get; }
        public BackpackAssetDescriptionAttribute[] Attributes { get; }
        public BackpackAssetDescriptionCapabilities Capabilities { get; }
        public string Class { get; }
        public BackpackAssetDescriptionClassSlot[] ClassSlots { get; }
        public string CraftClass { get; }
        public string CraftMaterialType { get; }
        public long DefinitionIndex { get; }
        public string Description { get; }
        public string DropType { get; }
        public BackpackAssetDescriptionEquippedSlot[] EquippedSlots { get; }
        public string HolidayRestriction { get; }
        public string ImageLargeUrl { get; }
        public string ImageUrl { get; }
        public bool IsCraftable { get; }
        public bool IsTradable { get; }
        public BackpackAssetDescriptionItemSet ItemSet { get; }
        public int Level { get; }
        public string MainSlotName { get; }
        public string Name { get; }
        public BackpackAssetDescriptionOrigin Origin { get; }
        public BackpackAssetDescriptionQuality Quality { get; }
        public BackpackAssetDescriptionStyle Style { get; }
        public string ToolType { get; }
        public string TypeName { get; }
        public string[] UsableByClasses { get; }
    }
}