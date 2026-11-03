using Anarchy.Toolkit.Core.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Core.Helper.System.Text.Json.Serialization;

public sealed class FlexibleLongListJsonConverter : JsonConverter<List<long>>
{
    public override void Write(Utf8JsonWriter writer, List<long> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteNumberValue(item);
        }

        writer.WriteEndArray();
    }

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
