using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extension;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Core.Helper.System.Text.Json.Serialization;

/// <summary>
/// Serializes/deserializes an enum to/from its <see cref="global::System.ComponentModel.DescriptionAttribute"/>
/// string representation.
/// </summary>
public class DescriptionEnumJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var description = reader.GetString()
                          ?? throw new EmptyJsonValueException(typeof(TEnum));

        return description.ToEnumValue<TEnum>();
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToDescription());
    }
}

/// <summary>
/// Creates a <see cref="DescriptionEnumJsonConverter{TEnum}"/> for any enum type, allowing the converter to be
/// referenced from a <see cref="JsonConverterAttribute"/> on generic members.
/// </summary>
public class DescriptionEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(
            typeof(DescriptionEnumJsonConverter<>).MakeGenericType(typeToConvert))!;
    }
}
