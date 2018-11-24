namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAssetDescriptionClassSlot
    {
        internal BackpackAssetDescriptionClassSlot(string className, string slotName)
        {
            ClassName = className;
            SlotName = slotName;
        }

        public string ClassName { get; }
        public string SlotName { get; }
    }
}