using Newtonsoft.Json;
using SDroid.SteamTrade.Helpers;
using SDroid.SteamTrade.Models.UserInventory;

namespace SDroid.SteamTrade.InternalModels.InventoryJson
{
    internal class InventoryAssetV1
    {
        [JsonProperty("amount")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long Amount { get; set; }

        [JsonProperty("id")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long AssetId { get; set; }

        [JsonProperty("classid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long ClassId { get; set; }

        [JsonProperty("instanceid")]
        [JsonConverter(typeof(JsonAsStringConverter<long>))]
        public long InstanceId { get; set; }

        [JsonProperty("pos")]
        public int Position { get; set; }

        public UserInventoryAsset ToSteamInventoryAsset(long appId, long contextId)
        {
            return new UserInventoryAsset(appId, contextId, AssetId, ClassId, InstanceId, Amount);
        }
    }
}