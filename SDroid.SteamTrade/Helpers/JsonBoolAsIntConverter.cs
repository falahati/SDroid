using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SDroid.SteamTrade.Helpers
{
    internal class JsonBoolAsIntConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanRead { get; } = true;

        /// <inheritdoc />
        public override bool CanWrite { get; } = true;

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return typeof(bool) == objectType;
        }

        /// <inheritdoc />
        // ReSharper disable once TooManyArguments
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            return JToken.ReadFrom(reader).Value<int>() != 0;
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, (bool) value ? 1 : 0);
        }
    }
}