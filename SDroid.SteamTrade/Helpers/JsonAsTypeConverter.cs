using System;
using Newtonsoft.Json;

namespace SDroid.SteamTrade.Helpers
{
    internal class JsonAsTypeConverter<TInterface, TConcrete> : JsonConverter
        where TInterface : class where TConcrete : class, TInterface
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
            return serializer.Deserialize<TConcrete>(reader);
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value as TConcrete);
        }
    }
}