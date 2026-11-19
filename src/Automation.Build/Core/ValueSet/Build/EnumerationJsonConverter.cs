using Fallout.Common.Tooling;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Automation.Build.Core.ValueSet.Build;

/// <summary>
/// Reads and writes Fallout enumeration values in parameter files as strings. A value is read by matching,
/// case-insensitively, either the name of one of the type's public static fields or that field's value.
/// </summary>
/// <typeparam name="T">The enumeration type to convert.</typeparam>
public sealed class EnumerationJsonConverter<T> : JsonConverter<T> where T : Enumeration
{
    /// <summary>
    /// Reads a JSON string as the matching value of <paramref name="typeToConvert"/>.
    /// </summary>
    /// <param name="reader">The reader positioned on the value.</param>
    /// <param name="typeToConvert">The enumeration type to read.</param>
    /// <param name="options">The serializer options (unused).</param>
    /// <returns>The matching enumeration value.</returns>
    /// <exception cref="System.Text.Json.JsonException">
    /// Thrown when the token is not a string, or no public static field matches it by name or value.
    /// </exception>
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected a string for {typeToConvert.Name}.");
        var text = reader.GetString();
        var value = typeToConvert.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => typeToConvert.IsAssignableFrom(field.FieldType))
            .Select(field => (field.Name, Value: field.GetValue(null) as T))
            .FirstOrDefault(item => string.Equals(item.Name, text, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(item.Value?.ToString(), text, StringComparison.OrdinalIgnoreCase));
        return value.Value ?? throw new JsonException($"Unknown {typeToConvert.Name} value '{text}'.");
    }

    /// <summary>
    /// Writes <paramref name="value"/> as a JSON string, using its <see cref="object.ToString"/>.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options (unused).</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}
