using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Core.Messaging.Abstractions.Entity.MessageBroker;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes.Exceptions;
using CloudNative.CloudEvents;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.Mime;

namespace Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes;

/// <summary>Maps domain event envelopes to CloudEvents 1.0 and back without coupling them to a transport.</summary>
/// <param name="defaultSource">Producer URI reference to use when a call does not supply one.</param>
/// <param name="aggregateTypeAttribute">Aggregate-type extension name; null uses the Anarchy default.</param>
/// <param name="persistedIdAttribute">Persisted-ID extension name; null uses the Anarchy default.</param>
public class CloudEventEnvelopeMapper(
    Uri? defaultSource = null,
    string? aggregateTypeAttribute = null,
    string? persistedIdAttribute = null) : ICloudEventEnvelopeMapper
{
    /// <summary>Default extension carrying the aggregate type.</summary>
    public const string AggregateTypeAttribute = "anarchyaggregatetype";

    /// <summary>Default extension carrying the optional storage-assigned event ID as a decimal string.</summary>
    public const string PersistedIdAttribute = "anarchypersistedid";

    /// <summary>The media type of the broker message body, not the CloudEvent data.</summary>
    public const string StructuredContentType = "application/cloudevents+json";

    private const string JsonContentType = "application/json";
    private const string ContextPrefix = "cloudevents.";

    /// <summary>The envelope custom-attribute key preserving the CloudEvent ID.</summary>
    public const string IdContextKey = ContextPrefix + "id";

    /// <summary>The envelope custom-attribute key preserving the CloudEvent producer URI reference.</summary>
    public const string SourceContextKey = ContextPrefix + "source";

    /// <summary>The envelope custom-attribute key preserving the CloudEvents specification version.</summary>
    public const string SpecVersionContextKey = ContextPrefix + "specversion";

    /// <summary>The envelope custom-attribute key preserving the CloudEvent data content type.</summary>
    public const string DataContentTypeContextKey = ContextPrefix + "datacontenttype";

    /// <summary>The envelope custom-attribute key preserving the CloudEvent data schema URI.</summary>
    public const string DataSchemaContextKey = ContextPrefix + "dataschema";

    /// <summary>The extension name used for the aggregate type in both directions.</summary>
    protected virtual string AggregateTypeAttributeName => aggregateTypeAttribute ?? AggregateTypeAttribute;

    /// <summary>The extension name used for the persisted ID in both directions.</summary>
    protected virtual string PersistedIdAttributeName => persistedIdAttribute ?? PersistedIdAttribute;

    /// <summary>The envelope custom-attribute key used to retain the CloudEvent ID.</summary>
    protected virtual string IdContextAttributeName => IdContextKey;

    /// <summary>The envelope custom-attribute key used to retain the CloudEvent producer URI reference.</summary>
    protected virtual string SourceContextAttributeName => SourceContextKey;

    /// <summary>The envelope custom-attribute key used to retain the CloudEvents specification version.</summary>
    protected virtual string SpecVersionContextAttributeName => SpecVersionContextKey;

    /// <summary>The envelope custom-attribute key used to retain the CloudEvent data content type.</summary>
    protected virtual string DataContentTypeContextAttributeName => DataContentTypeContextKey;

    /// <summary>The envelope custom-attribute key used to retain the CloudEvent data schema URI.</summary>
    protected virtual string DataSchemaContextAttributeName => DataSchemaContextKey;

    /// <inheritdoc/>
    public CloudEvent ToCloudEvent(IEventEnvelope envelope, Uri? source = null,
                                  string? dataContentType = null, Uri? dataSchema = null)
        => CreateCloudEvent(envelope, null, source, dataContentType, dataSchema);

    /// <inheritdoc/>
    public CloudEvent ToCloudEvent(IEventEnvelope envelope, string id, Uri? source = null,
                                  string? dataContentType = null, Uri? dataSchema = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new EmptyArgumentException(nameof(id));
        return CreateCloudEvent(envelope, id, source, dataContentType, dataSchema);
    }

    private CloudEvent CreateCloudEvent(IEventEnvelope envelope, string? id, Uri? source,
                                        string? dataContentType, Uri? dataSchema)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var version = MapSpecVersionToCloudEvent(envelope);
        var (aggregateName, persistedIdName, contextKeys) = GetMappingAttributeNames(version);
        var result = new CloudEvent(version) {
            Id = MapIdToCloudEvent(envelope, id),
            Source = MapSourceToCloudEvent(envelope, source),
            Type = MapTypeToCloudEvent(envelope),
            Subject = MapSubjectToCloudEvent(envelope),
            Time = MapTimeToCloudEvent(envelope),
            DataContentType = MapDataContentTypeToCloudEvent(envelope, dataContentType),
            DataSchema = MapDataSchemaToCloudEvent(envelope, dataSchema),
            Data = MapDataToCloudEvent(envelope),
            [aggregateName] = MapAggregateTypeToCloudEvent(envelope),
            [persistedIdName] = MapPersistedIdToCloudEvent(envelope)
        };

        foreach (var (name, value) in MapCustomAttributesToCloudEvent(envelope))
        {
            if (contextKeys.Contains(name)) continue;
            ValidateExtensionName(name, version);
            if (name == aggregateName || name == persistedIdName || value is null || result.GetAttribute(name) is not null)
                throw new InvalidCloudEventMappingException(name);

            result[name] = value;
        }

        return result;
    }

    /// <inheritdoc/>
    public DomainEventEnvelope ToEnvelope(CloudEvent cloudEvent)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);
        var version = MapSpecVersionFromCloudEvent(cloudEvent);
        var (aggregateName, persistedIdName, contextKeys) = GetMappingAttributeNames(cloudEvent.SpecVersion);
        var eventId = MapIdFromCloudEvent(cloudEvent);
        var source = MapSourceFromCloudEvent(cloudEvent);
        var dataContentType = MapDataContentTypeFromCloudEvent(cloudEvent);
        var dataSchema = MapDataSchemaFromCloudEvent(cloudEvent);
        var type = MapTypeFromCloudEvent(cloudEvent);
        var subject = MapSubjectFromCloudEvent(cloudEvent);
        var time = MapTimeFromCloudEvent(cloudEvent);
        var data = MapDataFromCloudEvent(cloudEvent);
        var aggregateType = MapAggregateTypeFromCloudEvent(cloudEvent, aggregateName);
        var persistedId = MapPersistedIdFromCloudEvent(cloudEvent, persistedIdName);
        var customAttributes = new Dictionary<string, string>(StringComparer.Ordinal) {
            [IdContextAttributeName] = eventId,
            [SourceContextAttributeName] = source.OriginalString,
            [SpecVersionContextAttributeName] = version.VersionId
        };
        if (dataContentType is not null) customAttributes.Add(DataContentTypeContextAttributeName, dataContentType);
        if (dataSchema is not null) customAttributes.Add(DataSchemaContextAttributeName, dataSchema.OriginalString);

        foreach (var (name, value) in MapCustomAttributesFromCloudEvent(cloudEvent, aggregateName, persistedIdName))
        {
            ValidateExtensionName(name, cloudEvent.SpecVersion);
            if (contextKeys.Contains(name) || name == aggregateName || name == persistedIdName || value is null ||
                !customAttributes.TryAdd(name, value))
                throw new InvalidCloudEventMappingException(name);
        }

        return persistedId is { } id
            ? new PersistedDomainEvent(id, aggregateType, subject, type, data, time, customAttributes)
            : new DomainEventEnvelope(aggregateType, subject, type, data, time, customAttributes);
    }

    /// <inheritdoc/>
    public CloudEvent ToCloudEvent(InboundMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return StructuredCloudEventJson.Decode(message.Message);
    }

    /// <inheritdoc/>
    public CloudEvent ToCloudEvent(OutboundMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return StructuredCloudEventJson.Decode(message.Message);
    }

    /// <inheritdoc/>
    public InboundMessage ToInboundMessage(CloudEvent cloudEvent, string messageId, string brokerSource,
                                          string brokerReference, string? partitionKey = null,
                                          IReadOnlyDictionary<string, string>? attributes = null)
        => new(messageId, brokerReference, StructuredCloudEventJson.Encode(cloudEvent), brokerSource, partitionKey, attributes);

    /// <inheritdoc/>
    public OutboundMessage ToOutboundMessage(CloudEvent cloudEvent, string messageId, string destination,
                                            string? partitionKey = null, string? deduplicationId = null,
                                            IReadOnlyDictionary<string, string>? attributes = null)
        => new(messageId, destination, StructuredCloudEventJson.Encode(cloudEvent), partitionKey, deduplicationId, attributes);

    protected virtual CloudEventsSpecVersion MapSpecVersionToCloudEvent(IEventEnvelope envelope)
        => GetRetainedContext(envelope, SpecVersionContextAttributeName) is null or "1.0"
            ? CloudEventsSpecVersion.V1_0
            : throw new InvalidCloudEventMappingException("specversion");

    protected virtual string MapIdToCloudEvent(IEventEnvelope envelope, string? id)
        => id ?? GetRetainedContext(envelope, IdContextAttributeName) ?? Guid.CreateVersion7().ToString();

    protected virtual Uri MapSourceToCloudEvent(IEventEnvelope envelope, Uri? source)
    {
        var result = source ?? GetRetainedUri(envelope, SourceContextAttributeName, UriKind.RelativeOrAbsolute) ?? defaultSource;
        return result is not null && !string.IsNullOrWhiteSpace(result.OriginalString)
            ? result
            : throw new EmptyArgumentException(nameof(source));
    }

    protected virtual string MapTypeToCloudEvent(IEventEnvelope envelope) => envelope.Type;

    protected virtual string? MapSubjectToCloudEvent(IEventEnvelope envelope) => envelope.Subject;

    protected virtual DateTimeOffset? MapTimeToCloudEvent(IEventEnvelope envelope) => envelope.Time;

    protected virtual string? MapDataContentTypeToCloudEvent(IEventEnvelope envelope, string? dataContentType)
        => ValidateJsonContentType(dataContentType ?? GetRetainedContext(envelope, DataContentTypeContextAttributeName) ?? JsonContentType);

    protected virtual Uri? MapDataSchemaToCloudEvent(IEventEnvelope envelope, Uri? dataSchema)
        => dataSchema ?? GetRetainedUri(envelope, DataSchemaContextAttributeName, UriKind.Absolute);

    protected virtual object? MapDataToCloudEvent(IEventEnvelope envelope) => ToJsonData(envelope.Data);

    protected virtual string MapAggregateTypeToCloudEvent(IEventEnvelope envelope) => envelope.AggregateType;

    protected virtual string? MapPersistedIdToCloudEvent(IEventEnvelope envelope)
        => (envelope as IPersistedEventEnvelope)?.Id.ToString(CultureInfo.InvariantCulture);

    protected virtual IReadOnlyDictionary<string, string> MapCustomAttributesToCloudEvent(IEventEnvelope envelope)
        => envelope.CustomAttributes;

    protected virtual CloudEventsSpecVersion MapSpecVersionFromCloudEvent(CloudEvent cloudEvent)
        => cloudEvent.SpecVersion == CloudEventsSpecVersion.V1_0
            ? cloudEvent.SpecVersion
            : throw new InvalidCloudEventMappingException("specversion");

    protected virtual string MapIdFromCloudEvent(CloudEvent cloudEvent) => RequiredString(cloudEvent.Id, "id");

    protected virtual Uri MapSourceFromCloudEvent(CloudEvent cloudEvent)
        => cloudEvent.Source is { } source && !string.IsNullOrWhiteSpace(source.OriginalString)
            ? source
            : throw new InvalidCloudEventMappingException("source");

    protected virtual string MapTypeFromCloudEvent(CloudEvent cloudEvent) => RequiredString(cloudEvent.Type, "type");

    protected virtual string MapSubjectFromCloudEvent(CloudEvent cloudEvent) => RequiredString(cloudEvent.Subject, "subject");

    protected virtual DateTimeOffset MapTimeFromCloudEvent(CloudEvent cloudEvent)
        => cloudEvent.Time ?? throw new InvalidCloudEventMappingException("time");

    protected virtual string? MapDataContentTypeFromCloudEvent(CloudEvent cloudEvent)
        => ValidateJsonContentType(cloudEvent.DataContentType);

    protected virtual Uri? MapDataSchemaFromCloudEvent(CloudEvent cloudEvent) => cloudEvent.DataSchema;

    protected virtual object? MapDataFromCloudEvent(CloudEvent cloudEvent) => ToJsonData(cloudEvent.Data);

    protected virtual string MapAggregateTypeFromCloudEvent(CloudEvent cloudEvent, string attributeName)
        => RequiredString(cloudEvent[attributeName] as string, attributeName);

    protected virtual long? MapPersistedIdFromCloudEvent(CloudEvent cloudEvent, string attributeName)
        => cloudEvent[attributeName] switch {
            null => null,
            string id when long.TryParse(id, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) => value,
            _ => throw new InvalidCloudEventMappingException(attributeName)
        };

    protected virtual IReadOnlyDictionary<string, string> MapCustomAttributesFromCloudEvent(
        CloudEvent cloudEvent, string aggregateTypeName, string persistedIdName)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (attribute, value) in cloudEvent.GetPopulatedAttributes())
        {
            if (!attribute.IsExtension || attribute.Name == aggregateTypeName || attribute.Name == persistedIdName) continue;

            result.Add(attribute.Name, attribute.Format(value));
        }

        return result;
    }

    internal static bool IsReservedName(string name, CloudEventsSpecVersion version)
        => name is "specversion" or "data" or "data_base64" || version.AllAttributes.Any(attribute => attribute.Name == name);

    private static string? GetRetainedContext(IEventEnvelope envelope, string name)
        => envelope.CustomAttributes.TryGetValue(name, out var value) ? RequiredString(value, name) : null;

    private static Uri? GetRetainedUri(IEventEnvelope envelope, string name, UriKind kind)
    {
        var value = GetRetainedContext(envelope, name);
        return value is null ? null : Uri.TryCreate(value, kind, out var uri)
            ? uri
            : throw new InvalidCloudEventMappingException(name);
    }

    private (string Aggregate, string PersistedId, HashSet<string> ContextKeys) GetMappingAttributeNames(CloudEventsSpecVersion version)
    {
        var aggregate = AggregateTypeAttributeName;
        var persisted = PersistedIdAttributeName;
        ValidateExtensionName(aggregate, version);
        ValidateExtensionName(persisted, version);
        if (aggregate == persisted) throw new InvalidCloudEventMappingException(persisted);
        var contextKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in new[] {
                     IdContextAttributeName, SourceContextAttributeName, SpecVersionContextAttributeName,
                     DataContentTypeContextAttributeName, DataSchemaContextAttributeName
                 })
        {
            if (string.IsNullOrWhiteSpace(key) || IsReservedName(key, version) || key == aggregate || key == persisted ||
                !contextKeys.Add(key))
                throw new InvalidCloudEventMappingException(key);
        }

        return (aggregate, persisted, contextKeys);
    }

    private static void ValidateExtensionName(string name, CloudEventsSpecVersion version)
    {
        if (IsReservedName(name, version)) throw new InvalidCloudEventMappingException(name);
        try { CloudEventAttribute.CreateExtension(name, CloudEventAttributeType.String); }
        catch (ArgumentException ex) { throw new InvalidCloudEventMappingException(name, ex); }
    }

    private static string RequiredString(string? value, string attribute)
        => !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidCloudEventMappingException(attribute);

    private static string? ValidateJsonContentType(string? value)
    {
        if (value is null) return null;

        ContentType contentType;
        try { contentType = new ContentType(value); }
        catch (FormatException ex) { throw new InvalidCloudEventMappingException("datacontenttype", ex); }
        if (!contentType.MediaType.Equals(JsonContentType, StringComparison.OrdinalIgnoreCase) &&
            !contentType.MediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidCloudEventMappingException("datacontenttype");
        return value;
    }

    private static JToken? ToJsonData(object? data) => data switch {
        null or JValue { Type: JTokenType.Null } => null,
        byte[] or JValue { Type: JTokenType.Bytes } => throw new InvalidCloudEventMappingException("data"),
        JToken token => token.DeepClone(),
        // Wrap scalar strings in a token; the envelope constructor treats strings as serialized JSON documents.
        _ => JToken.FromObject(data)
    };
}
