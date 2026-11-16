using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Web.Presentation.Converter.Json;

/// <summary>
/// JSON converter factory that serializes enums (and nullable enums) by their description instead of their numeric
/// value or name.
/// </summary>
/// <remarks>
/// <para>
/// Writing uses the toolkit <c>ToDescription()</c> extension, falling back to the enum name when it fails. Reading
/// accepts only JSON strings and resolves them with <c>TryToEnumValue</c>.
/// </para>
/// <para>
/// For non-nullable enums, a non-string token throws <see cref="Anarchy.Toolkit.Core.Exceptions.UnexpectedJsonTokenException"/>,
/// an empty string throws <see cref="Anarchy.Toolkit.Core.Exceptions.EmptyJsonValueException"/> and an unknown value throws
/// <see cref="Anarchy.Toolkit.Core.Exceptions.InvalidJsonValueException"/>. For nullable enums, JSON <c>null</c> and
/// empty strings read as <see langword="null"/>. Registered globally by <c>WebAppStartupBase</c>.
/// </para>
/// </remarks>
public sealed class DescribableEnumJsonConverter : JsonConverterFactory
{
    /// <summary>Determines whether <paramref name="typeToConvert"/> is an enum or a nullable enum.</summary>
    /// <param name="typeToConvert">The type to check.</param>
    /// <returns><see langword="true"/> for enum and nullable enum types.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        var underlying = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        return underlying.IsEnum;
    }

    /// <summary>Creates the description-based converter for the given (nullable) enum type.</summary>
    /// <param name="typeToConvert">The enum or nullable enum type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>A converter for <paramref name="typeToConvert"/>.</returns>
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
