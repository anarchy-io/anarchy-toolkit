using Fallout.Common.Tooling;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Build.Fallout.Core.ValueSet.Build;

/// <summary>
/// Reads and writes Fallout enumeration values in parameter files as strings. A value is read by matching,
/// case-insensitively, either the name of one of the type's public static fields or that field's value.
/// </summary>
/// <typeparam name="T">The enumeration type to convert.</typeparam>
public sealed class EnumerationJsonConverter<T> : JsonConverter<T> where T : Enumeration
{
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

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}
