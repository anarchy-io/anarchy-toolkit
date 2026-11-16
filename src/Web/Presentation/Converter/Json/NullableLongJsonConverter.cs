using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Web.Presentation.Converter.Json;

/// <summary>
/// Converts nullable <see langword="long"/> values using <c>-1</c> as the JSON sentinel for <see langword="null"/>.
/// </summary>
/// <remarks>Registered globally for System.Text.Json by <c>WebAppStartupBase</c>.</remarks>
public class NullableLongJsonConverter : JsonConverter<long?>
{
    /// <summary>Reads a JSON number; <c>-1</c> becomes <see langword="null"/>.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The value, or <see langword="null"/> for <c>-1</c>.</returns>
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetInt64();
        return value == -1 ? null : value;
    }

    /// <summary>Writes the value as a JSON number; <see langword="null"/> is written as <c>-1</c>.</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value ?? -1);
    }
}
