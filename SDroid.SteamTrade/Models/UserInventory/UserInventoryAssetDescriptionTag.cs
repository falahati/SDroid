namespace SDroid.SteamTrade.Models.UserInventory
{
    public class UserInventoryAssetDescriptionTag
    {
        internal UserInventoryAssetDescriptionTag(
            string name,
            string localizeName,
            string category,
            string localizeCategoryName)
        {
            Name = name;
            LocalizeName = localizeName;
            Category = category;
            LocalizeCategoryName = localizeCategoryName;
        }

        public string Category { get; }

        public string LocalizeCategoryName { get; }

        public string LocalizeName { get; }
        public string Name { get; }
    }
}