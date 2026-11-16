using Anarchy.Toolkit.Core.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Core.Helper.System.Text.Json.Serialization;

/// <summary>
/// System.Text.Json converter for <see cref="List{T}"/> of <see cref="long"/> that accepts both numeric and string
/// array elements (e.g. <c>[1, "2", 3]</c>) when reading, and always writes numbers.
/// </summary>
/// <example>
/// <code language="csharp">
/// public sealed class Request
/// {
///     [JsonConverter(typeof(FlexibleLongListJsonConverter))]
///     public List&lt;long&gt; Ids { get; init; } = [];
/// }
/// </code>
/// </example>
public sealed class FlexibleLongListJsonConverter : JsonConverter<List<long>>
{
    /// <summary>
    /// Writes the list as a JSON array of numbers.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The list to write.</param>
    /// <param name="options">The serializer options (unused).</param>
    public override void Write(Utf8JsonWriter writer, List<long> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteNumberValue(item);
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Reads a JSON array whose elements are numbers or numeric strings.
    /// </summary>
    /// <param name="reader">The JSON reader, positioned on the start of the array.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">The serializer options (unused).</param>
    /// <returns>The parsed list.</returns>
    /// <exception cref="UnexpectedJsonTokenException">
    /// The value is not an array, or an element is neither a number nor a string.
    /// </exception>
    /// <exception cref="UnexpectedJsonEndException">The input ends before the array is closed.</exception>
    /// <exception cref="FormatException">A string element is not a valid <see cref="long"/>.</exception>
    public override List<long> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new UnexpectedJsonTokenException(reader.TokenType, typeToConvert);
        }

        var result = new List<long>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return result;
            }

            result.Add(reader.TokenType switch {
                JsonTokenType.Number => reader.GetInt64(),
                JsonTokenType.String => long.Parse(reader.GetString()!),
                _ => throw new UnexpectedJsonTokenException(reader.TokenType, typeToConvert)
            });
        }

        throw new UnexpectedJsonEndException(typeToConvert);
    }
}
