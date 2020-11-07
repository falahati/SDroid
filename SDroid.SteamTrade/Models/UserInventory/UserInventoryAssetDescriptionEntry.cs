using System.Collections.Generic;
using System.Linq;

namespace SDroid.SteamTrade.Models.UserInventory
{
    public class UserInventoryAssetDescriptionEntry
    {
        private readonly Dictionary<string, string> _appData;

        internal UserInventoryAssetDescriptionEntry(string type, string value, Dictionary<string, string> appData)
        {
            Type = type;
            Value = value;
            _appData = appData;
        }

        public Dictionary<string, string> AppData
        {
            get => _appData?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<string, string>();
        }

        public uint? DefinitionIndex
        {
            get => _appData?.ContainsKey("def_index") == true && uint.TryParse(_appData["def_index"], out var defIndex)
                ? defIndex
                : (uint?) null;
        }

        public bool IsItemSetName
        {
            get => _appData?.ContainsKey("is_itemset_name") == true && _appData["is_itemset_name"] == "1";
        }

        public string Type { get; }

        public string Value { get; }
    }
}