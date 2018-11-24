namespace SDroid.SteamTrade.Models.Backpack
{
    public class BackpackAssetDescriptionQuality
    {
        internal BackpackAssetDescriptionQuality(int id, string key, string name)
        {
            Id = id;
            Key = key;
            Name = name;
        }

        public int Id { get; }
        public string Key { get; }
        public string Name { get; }
    }
}