using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SDroid.SteamTrade.Helpers
{
    internal class JsonObjactableArrayConverter<T> : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanRead { get; } = true;

        /// <inheritdoc />
        public override bool CanWrite { get; } = true;

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsArray;
        }

        /// <inheritdoc />
        // ReSharper disable once TooManyArguments
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer
        )
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.Object)
            {
                return new [] {token.ToObject<T>()};
            }

            return token.ToObject<T[]>();
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}