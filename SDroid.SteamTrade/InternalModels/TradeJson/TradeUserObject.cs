using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SDroid.SteamTrade.Helpers;

namespace SDroid.SteamTrade.InternalModels.TradeJson
{
    internal class TradeUserObject
    {
        [JsonProperty("assets")]
        public JContainer Assets { get; set; }

        [JsonProperty("confirmed")]
        [JsonConverter(typeof(JsonBoolAsIntConverter))]
        public bool Confirmed { get; set; }

        [JsonProperty("currency")]
        public JContainer Currencies { get; set; }

        [JsonProperty("connection_pending")]
        public bool IsConnectionPending { get; set; }

        [JsonProperty("ready")]
        [JsonConverter(typeof(JsonBoolAsIntConverter))]
        public bool Ready { get; set; }

        [JsonProperty("sec_since_touch")]
        public int SecondsSinceTouch { get; set; }

        public Tuple<int?, TradeUserAsset>[] GetAssets()
        {
            if (Assets is JArray assetsArray)
            {
                // if items were added in trade the type is an array like so:
                // a normal JSON array
                // "assets": [
                //    {
                //        "assetid": "1693638354", <snip>
                //    }
                // ],
                return assetsArray.Value<TradeUserAsset[]>()
                    .Select(asset => new Tuple<int?, TradeUserAsset>(null, asset))
                    .ToArray();
            }

            if (Assets is JObject assetsDictionary)
            {
                // when items are removed from trade they look like this:
                // a JSON object like a "list"
                // (item in trade slot 1 was removed)
                // "assets": {
                //    "2": {
                //        "assetid": "1745718856", <snip>
                //    },
                //    "3": {
                //        "assetid": "1690644335", <snip>
                //    }
                // },
                return assetsDictionary.Value<Dictionary<int, TradeUserAsset>>()
                    .Select(pair => new Tuple<int?, TradeUserAsset>(pair.Key, pair.Value))
                    .ToArray();
            }

            return new Tuple<int?, TradeUserAsset>[0];
        }

        public Tuple<int?, TradeUserCurrency>[] GetCurrencies()
        {
            if (Currencies is JArray assetsArray)
            {
                // if items were added in trade the type is an array like so:
                // a normal JSON array
                // "assets": [
                //    {
                //        "currencyid": "1693638354", <snip>
                //    }
                // ],
                return assetsArray.Value<TradeUserCurrency[]>()
                    .Select(asset => new Tuple<int?, TradeUserCurrency>(null, asset))
                    .ToArray();
            }

            if (Currencies is JObject assetsDictionary)
            {
                // when items are removed from trade they look like this:
                // a JSON object like a "list"
                // (item in trade slot 1 was removed)
                // "assets": {
                //    "2": {
                //        "currencyid": "1745718856", <snip>
                //    },
                //    "3": {
                //        "currencyid": "1690644335", <snip>
                //    }
                // },
                return assetsDictionary.Value<Dictionary<int, TradeUserCurrency>>()
                    .Select(pair => new Tuple<int?, TradeUserCurrency>(pair.Key, pair.Value))
                    .ToArray();
            }

            return new Tuple<int?, TradeUserCurrency>[0];
        }
    }
}