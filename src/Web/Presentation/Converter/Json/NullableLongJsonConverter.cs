using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Web.Presentation.Converter.Json;

public class NullableLongJsonConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetInt64();
        return value == -1 ? null : value;
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value ?? -1);
    }
}
