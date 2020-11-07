namespace SDroid.SteamTrade.Models.UserInventory
{
    public class UserInventoryAssetDescriptionAction
    {
        internal UserInventoryAssetDescriptionAction(
            string title,
            string link
            )
        {
            Title = title;
            Link = link;
        }

        public string Title { get; }

        public string Link { get; }
    }
}