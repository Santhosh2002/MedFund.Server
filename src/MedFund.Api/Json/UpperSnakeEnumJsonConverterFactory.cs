using System.Text.Json;
using System.Text.Json.Serialization;
using MedFund.Application.Common;

namespace MedFund.Api.Json;

public sealed class UpperSnakeEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(UpperSnakeEnumJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class UpperSnakeEnumJsonConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (TryParse(value, out var parsed))
            {
                return parsed;
            }

            throw new JsonException($"Value '{value}' is not valid for {typeof(TEnum).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(RoleNames.ToUpperSnakeCase(value.ToString()));
        }

        private static bool TryParse(string? value, out TEnum parsed)
        {
            parsed = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Replace("_", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
            foreach (var name in Enum.GetNames<TEnum>())
            {
                if (string.Equals(name, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    parsed = Enum.Parse<TEnum>(name);
                    return true;
                }
            }

            return false;
        }
    }
}
