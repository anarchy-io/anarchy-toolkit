using Anarchy.Toolkit.Core.Extensions.System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Runtime.Web.Presentation.Converter.Json;

/// <summary>
/// Converts nullable <see cref="DateTimeOffset"/> values from and to JSON strings, treating empty strings as
/// <see langword="null"/>.
/// </summary>
/// <remarks>
/// Registered globally for System.Text.Json by <c>WebAppStartupBase</c>. Parsing and formatting use the current
/// culture (<see cref="DateTimeOffset.Parse(string)"/> and <see cref="DateTimeOffset.ToString()"/>), and
/// <see langword="null"/> is written as an empty string.
/// </remarks>
public class NullableDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset?>
{
    /// <summary>Reads a JSON string; <see langword="null"/> or empty becomes <see langword="null"/>, otherwise it is parsed.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The parsed value, or <see langword="null"/>.</returns>
    /// <exception cref="FormatException">The string is not a valid date and time.</exception>
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value.IsNullOrEmpty() ? null : DateTimeOffset.Parse(value!);
    }

    /// <summary>Writes the value's string representation.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
