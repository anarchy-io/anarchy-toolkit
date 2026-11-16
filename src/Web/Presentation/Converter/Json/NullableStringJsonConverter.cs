using Anarchy.Toolkit.Core.Extension;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Web.Presentation.Converter.Json;

/// <summary>
/// Converts strings so that empty JSON strings are read as <see langword="null"/> and <see langword="null"/>
/// values are written as empty strings.
/// </summary>
/// <remarks>Registered globally for System.Text.Json by <c>WebAppStartupBase</c>.</remarks>
public class NullableStringJsonConverter : JsonConverter<string?>
{
    /// <summary>Reads a JSON string; <see langword="null"/> or empty becomes <see langword="null"/>.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The string, or <see langword="null"/> when empty.</returns>
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value.IsNullOrEmpty() ? null : value;
    }

    /// <summary>Writes the value as a JSON string; <see langword="null"/> is written as an empty string.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.IsNullOrEmpty() ? string.Empty : value);
    }
}
