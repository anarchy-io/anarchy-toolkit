using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Extensions.System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Core.Serialization.Json;

/// <summary>
/// Serializes/deserializes an enum to/from its <see cref="global::System.ComponentModel.DescriptionAttribute"/>
/// string representation.
/// </summary>
/// <remarks>
/// Relies on <see cref="EnumExtensions"/>: reading is case-sensitive and every value written must declare a
/// description.
/// </remarks>
/// <typeparam name="TEnum">The enum type to convert.</typeparam>
/// <example>
/// <code language="csharp">
/// [JsonConverter(typeof(DescriptionEnumJsonConverter&lt;Status&gt;))]
/// public enum Status
/// {
///     [Description("active")] Active,
///     [Description("inactive")] Inactive
/// }
/// </code>
/// </example>
public class DescriptionEnumJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    /// <summary>
    /// Reads a JSON string and converts it to the <typeparamref name="TEnum"/> value with a matching description.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options (unused).</param>
    /// <returns>The matching enum value.</returns>
    /// <exception cref="EmptyJsonValueException">The JSON value is <c>null</c>.</exception>
    /// <exception cref="InvalidEnumDescriptionException">
    /// No value of <typeparamref name="TEnum"/> has the given description.
    /// </exception>
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var description = reader.GetString()
                          ?? throw new EmptyJsonValueException(typeof(TEnum));

        return description.ToEnumValue<TEnum>();
    }

    /// <summary>
    /// Writes the description of <paramref name="value"/> as a JSON string.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The enum value to write.</param>
    /// <param name="options">The serializer options (unused).</param>
    /// <exception cref="MissingEnumDescriptionException">The value has no description.</exception>
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToDescription());
    }
}

/// <summary>
/// Creates a <see cref="DescriptionEnumJsonConverter{TEnum}"/> for any enum type, allowing the converter to be
/// referenced from a <see cref="JsonConverterAttribute"/> on generic members.
/// </summary>
/// <example>
/// <code language="csharp">
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new DescriptionEnumJsonConverterFactory());
/// </code>
/// </example>
public class DescriptionEnumJsonConverterFactory : JsonConverterFactory
{
    /// <summary>
    /// Determines whether the factory can create a converter for <paramref name="typeToConvert"/>.
    /// </summary>
    /// <param name="typeToConvert">The type to check.</param>
    /// <returns><see langword="true"/> for any enum type.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    /// <summary>
    /// Creates a <see cref="DescriptionEnumJsonConverter{TEnum}"/> closed over <paramref name="typeToConvert"/>.
    /// </summary>
    /// <param name="typeToConvert">The enum type.</param>
    /// <param name="options">The serializer options (unused).</param>
    /// <returns>The converter instance.</returns>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(
            typeof(DescriptionEnumJsonConverter<>).MakeGenericType(typeToConvert))!;
    }
}
