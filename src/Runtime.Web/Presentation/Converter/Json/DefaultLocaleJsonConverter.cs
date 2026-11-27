using Anarchy.Toolkit.Core.Extensions.System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Runtime.Web.Presentation.Converter.Json;

/// <summary>
/// Converts locale strings, defaulting <see langword="null"/> or empty JSON values to <c>nl_NL</c> when reading.
/// </summary>
/// <remarks>Not registered globally; apply it with <see cref="JsonConverterAttribute"/> where needed.</remarks>
public class DefaultLocaleJsonConverter : JsonConverter<string?>
{
    /// <summary>Reads a JSON string; <see langword="null"/> or empty becomes <c>nl_NL</c>.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The locale.</returns>
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value.IsNullOrEmpty() ? "nl_NL" : value;
    }

    /// <summary>Writes the value as a JSON string.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
