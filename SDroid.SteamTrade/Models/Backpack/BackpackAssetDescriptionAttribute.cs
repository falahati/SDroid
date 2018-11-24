using SDroid.SteamTrade.InternalModels.EconomyItemsAPI;

namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAssetDescriptionAttribute
    {
        internal BackpackAssetDescriptionAttribute(
            SchemaOverviewAttribute attributeOverview,
            SchemaAttribute attribute) : this(attributeOverview)
        {
            Value = attribute?.AttributeValue;
            NumericValue =
                !string.IsNullOrWhiteSpace(attribute?.AttributeValue) &&
                decimal.TryParse(attribute.AttributeValue, out var result)
                    ? result
                    : (decimal?) null;
            AccountInfo = null;
        }

        internal BackpackAssetDescriptionAttribute(SchemaOverviewAttribute attributeOverview)
        {
            AttributeName = attributeOverview?.AttributeName;
            AttributeClass = attributeOverview?.AttributeClass;
            DefinitionIndex = attributeOverview?.DefinitionIndex ?? -1;
            DescriptionString = attributeOverview?.DescriptionString;
            DescriptionFormat = attributeOverview?.DescriptionFormat;
            EffectType = attributeOverview?.EffectType;
            IsHidden = attributeOverview?.IsHidden ?? true;
            IsStoredAsInteger = attributeOverview?.IsStoredAsInteger ?? false;
        }

        internal BackpackAssetDescriptionAttribute(
            SchemaOverviewAttribute attributeOverview,
            ItemAttribute attribute) : this(attributeOverview)
        {
            Value = attribute?.AttributeValue;
            NumericValue = attribute?.NumericValue;
            AccountInfo = attribute?.AccountInfo != null
                ? new BackpackAssetDescriptionAttributeAccount(attribute.AccountInfo)
                : null;
        }

        public BackpackAssetDescriptionAttributeAccount AccountInfo { get; }
        public string AttributeClass { get; }
        public string AttributeName { get; }
        public int DefinitionIndex { get; }
        public string DescriptionFormat { get; }
        public string DescriptionString { get; }
        public string EffectType { get; }
        public bool IsHidden { get; }
        public bool IsStoredAsInteger { get; }
        public decimal? NumericValue { get; }
        public string Value { get; }
    }
}