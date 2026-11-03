using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Web.Presentation.Converter.Json;

public class NullableFloatJsonConverter : JsonConverter<float?>
{
    public override float? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetSingle();
        return value == 0 ? null : value;
    }

    public override void Write(Utf8JsonWriter writer, float? value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value ?? 0);
    }
}
