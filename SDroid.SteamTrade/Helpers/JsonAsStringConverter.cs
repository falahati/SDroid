using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SDroid.SteamTrade.Helpers
{
    internal class JsonAsStringConverter<T> : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanRead { get; } = true;

        /// <inheritdoc />
        public override bool CanWrite { get; } = true;

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <inheritdoc />
        // ReSharper disable once TooManyArguments
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            if (typeof(T).IsValueType)
            {
                return JToken.ReadFrom(reader).Value<T>();
            }

            return JsonConvert.DeserializeObject<T>(JToken.ReadFrom(reader).Value<string>());
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer,
                typeof(T).IsValueType ||
                typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>)
                    ? value?.ToString()
                    : JsonConvert.SerializeObject(value));
        }
    }
}