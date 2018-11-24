namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAssetDescriptionEquippedSlot
    {
        internal BackpackAssetDescriptionEquippedSlot(int classId, int slotId)
        {
            ClassId = classId;
            SlotId = slotId;
        }

        public int ClassId { get; }
        public int SlotId { get; }
    }
}