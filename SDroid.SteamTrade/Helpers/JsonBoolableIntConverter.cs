using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SDroid.SteamTrade.Helpers
{
    internal class JsonBoolableIntConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanRead { get; } = true;

        /// <inheritdoc />
        public override bool CanWrite { get; } = true;

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return typeof(int) == objectType;
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
            if (token.Type == JTokenType.Integer)
            {
                return token.ToObject<int>();
            }

            return 0;
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, (int)value == 0 ? false : value);
        }
    }
}