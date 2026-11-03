using Anarchy.Toolkit.Core.Extension;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Anarchy.Toolkit.Web.Presentation.Converter.Json;

public class DefaultLocaleJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value.IsNullOrEmpty() ? "nl_NL" : value;
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
