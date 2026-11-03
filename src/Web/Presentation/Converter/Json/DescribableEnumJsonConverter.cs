using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Web.Presentation.Converter.Json;

public sealed class DescribableEnumJsonConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        var underlying = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        return underlying.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlying = Nullable.GetUnderlyingType(typeToConvert);
        var enumType = underlying ?? typeToConvert;
        var isNullable = underlying is not null;

        var converterType = isNullable ?
            typeof(NullableDescribableEnumConverter<>).MakeGenericType(enumType) :
            typeof(DescribableEnumConverter<>).MakeGenericType(enumType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class DescribableEnumConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new UnexpectedJsonTokenException(reader.TokenType, typeof(TEnum));

            var description = reader.GetString();

            if (string.IsNullOrWhiteSpace(description))
                throw new EmptyJsonValueException(typeof(TEnum));

            return !description.TryToEnumValue<TEnum>(out var value) ?
                throw new InvalidJsonValueException(description, typeof(TEnum)) :
                value;
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            try
            {
                writer.WriteStringValue(value.ToDescription());
            }
            catch
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }

    private sealed class NullableDescribableEnumConverter<TEnum> : JsonConverter<TEnum?>
        where TEnum : struct, Enum
    {
        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
                throw new UnexpectedJsonTokenException(reader.TokenType, typeof(TEnum));

            var description = reader.GetString();

            if (string.IsNullOrWhiteSpace(description))
                return null;

            return !description.TryToEnumValue<TEnum>(out var value) ?
                throw new InvalidJsonValueException(description, typeof(TEnum)) :
                value;
        }

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            try
            {
                writer.WriteStringValue(value?.ToDescription());
            }
            catch
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }
}
