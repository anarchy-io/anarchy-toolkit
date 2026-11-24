using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes.Exceptions;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes;

internal static class StructuredCloudEventJson
{
    public static string Encode(CloudEvent cloudEvent)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);
        foreach (var (attribute, _) in cloudEvent.GetPopulatedAttributes())
        {
            if (attribute.IsExtension && CloudEventEnvelopeMapper.IsReservedName(attribute.Name, cloudEvent.SpecVersion))
                throw new InvalidCloudEventMappingException(attribute.Name);
        }

        try
        {
            var bytes = new StructuredJsonFormatter().EncodeStructuredModeMessage(cloudEvent, out _);
            return Encoding.UTF8.GetString(bytes.Span);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or JsonException)
        {
            throw new InvalidCloudEventMappingException("body", ex);
        }
    }

    public static CloudEvent Decode(string body)
    {
        try
        {
            using var reader = new JsonTextReader(new StringReader(body)) { DateParseHandling = DateParseHandling.None };
            var json = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read()) throw new InvalidCloudEventMappingException("body");
            var extensions = GetTypedExtensions(json);
            return new StructuredJsonFormatter().ConvertFromJObject(json, extensions);
        }
        catch (Exception ex) when (ex is not InvalidCloudEventMappingException &&
                                   ex is (ArgumentException or FormatException or JsonException or OverflowException))
        {
            throw new InvalidCloudEventMappingException("body", ex);
        }
    }

    private static IEnumerable<CloudEventAttribute> GetTypedExtensions(JObject json)
    {
        if (json["specversion"] is not JValue { Type: JTokenType.String, Value: string versionId } ||
            CloudEventsSpecVersion.FromVersionId(versionId) is not { } version)
            yield break; // Let the SDK report an absent or unsupported version.

        foreach (var property in json.Properties())
        {
            if (CloudEventEnvelopeMapper.IsReservedName(property.Name, version)) continue;

            var type = property.Value.Type switch {
                JTokenType.Boolean => CloudEventAttributeType.Boolean,
                JTokenType.Integer => CloudEventAttributeType.Integer,
                _ => null
            };
            if (type is not null) yield return CloudEventAttribute.CreateExtension(property.Name, type);
        }
    }

    private sealed class StructuredJsonFormatter : JsonEventFormatter
    {
        protected override bool IsJsonMediaType(string mediaType)
            => mediaType.EndsWith("/json", StringComparison.OrdinalIgnoreCase) ||
               mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }
}
