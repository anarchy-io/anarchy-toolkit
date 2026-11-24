using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Core.Exceptions;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes.Exceptions;
using AwesomeAssertions;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.CloudEvents.Envelopes;

public class CloudEventEnvelopeMapperTest
{
    private static readonly DateTimeOffset Moment = new(2025, 2, 3, 4, 5, 6, TimeSpan.FromHours(2));
    private static readonly Uri Source = new("/orders", UriKind.Relative);
    private static readonly CloudEventEnvelopeMapper Mapper = new(Source);

    [Fact]
    public void StructuredJson_WhenEnvelopeIsPersisted_RoundTripsDataAndMetadata()
    {
        var attributes = new Dictionary<string, string> { ["traceparent"] = "trace-value" };
        var envelope = new PersistedDomainEvent(long.MaxValue, "order", "order/42", "order.created", new { Total = 10 }, Moment, attributes);
        var schema = new Uri("https://example.test/schemas/order.json");
        var cloudEvent = Mapper.ToCloudEvent(envelope, "event-1", dataContentType: "application/vnd.order+json", dataSchema: schema);
        var formatter = new JsonEventFormatter();

        var bytes = formatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);
        var json = JObject.Parse(Encoding.UTF8.GetString(bytes.Span));
        var received = formatter.DecodeStructuredModeMessage(bytes, contentType, null);
        var restored = Mapper.ToEnvelope(received);
        var remapped = new CloudEventEnvelopeMapper().ToCloudEvent(restored);

        contentType.MediaType.Should().Be("application/cloudevents+json");
        json["specversion"]!.Value<string>().Should().Be("1.0");
        json["id"]!.Value<string>().Should().Be("event-1");
        json["source"]!.Value<string>().Should().Be("/orders");
        json["type"]!.Value<string>().Should().Be("order.created");
        json["subject"]!.Value<string>().Should().Be("order/42");
        json["anarchyaggregatetype"]!.Value<string>().Should().Be("order");
        json["anarchypersistedid"]!.Value<string>().Should().Be(long.MaxValue.ToString());
        json["data"]!["Total"]!.Value<int>().Should().Be(10);
        received["traceparent"].Should().Be("trace-value");
        restored.Should().BeOfType<PersistedDomainEvent>().Which.Id.Should().Be(long.MaxValue);
        restored.Subject.Should().Be(envelope.Subject);
        restored.AggregateType.Should().Be(envelope.AggregateType);
        restored.Type.Should().Be(envelope.Type);
        restored.Time.Should().Be(Moment);
        restored.CastData<OrderData>().Total.Should().Be(10);
        restored.CustomAttributes.Should().Contain("traceparent", "trace-value");
        restored.CustomAttributes.Should().Contain(CloudEventEnvelopeMapper.IdContextKey, "event-1");
        restored.CustomAttributes.Should().Contain(CloudEventEnvelopeMapper.SourceContextKey, "/orders");
        restored.CustomAttributes.Should().Contain(CloudEventEnvelopeMapper.SpecVersionContextKey, "1.0");
        remapped.Id.Should().Be("event-1");
        remapped.Source.Should().Be(Source);
        remapped.DataSchema.Should().Be(schema);
        remapped.DataContentType.Should().Be("application/vnd.order+json");
        remapped["traceparent"].Should().Be("trace-value");
    }

    [Fact]
    public void ToEnvelope_WhenExternalStructuredEventHasStringData_PreservesJsonScalar()
    {
        const string body = """
            {"specversion":"1.0","id":"external-1","source":"https://example.test/orders",
             "type":"order.created","subject":"42","time":"2025-02-03T02:05:06Z",
             "anarchyaggregatetype":"order","data":"not a JSON document"}
            """;
        var formatter = new JsonEventFormatter();
        var cloudEvent = formatter.DecodeStructuredModeMessage(Encoding.UTF8.GetBytes(body), null, null);

        var result = Mapper.ToEnvelope(cloudEvent);

        result.Should().BeOfType<DomainEventEnvelope>();
        ((JValue)result.Data!).Value.Should().Be("not a JSON document");
    }

    [Fact]
    public void StructuredJson_WhenPayloadIsNull_RoundTripsWithoutInventingData()
    {
        var envelope = new DomainEventEnvelope("order", "42", "created", time: Moment);
        var formatter = new JsonEventFormatter();
        var bytes = formatter.EncodeStructuredModeMessage(
            Mapper.ToCloudEvent(envelope, "1"), out var contentType);

        var result = Mapper.ToEnvelope(formatter.DecodeStructuredModeMessage(bytes, contentType, null));

        result.Data.Should().BeNull();
        result.Should().BeOfType<DomainEventEnvelope>();
    }

    [Fact]
    public void StructuredJson_WhenPayloadIsAnArray_PreservesJsonData()
    {
        var envelope = new DomainEventEnvelope("order", "42", "created", new[] { 1, 2 }, Moment);
        var formatter = new JsonEventFormatter();
        var bytes = formatter.EncodeStructuredModeMessage(
            Mapper.ToCloudEvent(envelope, "1"), out var contentType);

        var result = Mapper.ToEnvelope(formatter.DecodeStructuredModeMessage(bytes, contentType, null));

        ((JArray)result.Data!).Values<int>().Should().Equal(1, 2);
    }

    [Fact]
    public void ToEnvelope_WhenDataIsJsonNull_ReturnsNullPayload()
    {
        var cloudEvent = ValidCloudEvent();
        cloudEvent.Data = JValue.CreateNull();

        var result = Mapper.ToEnvelope(cloudEvent);

        result.Data.Should().BeNull();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("source")]
    [InlineData("type")]
    [InlineData("subject")]
    [InlineData("time")]
    [InlineData(CloudEventEnvelopeMapper.AggregateTypeAttribute)]
    public void ToEnvelope_WhenMappingMetadataIsMissing_RejectsIt(string attribute)
    {
        var cloudEvent = ValidCloudEvent();
        cloudEvent[attribute] = null;

        var act = () => Mapper.ToEnvelope(cloudEvent);

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be(attribute);
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("9223372036854775808")]
    [InlineData("")]
    public void ToEnvelope_WhenStorageIdIsInvalid_RejectsIt(string id)
    {
        var cloudEvent = ValidCloudEvent();
        cloudEvent[CloudEventEnvelopeMapper.PersistedIdAttribute] = id;

        var act = () => Mapper.ToEnvelope(cloudEvent);

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be(CloudEventEnvelopeMapper.PersistedIdAttribute);
    }

    [Fact]
    public void ToEnvelope_WhenContentIsNotJson_RejectsIt()
    {
        var cloudEvent = ValidCloudEvent();
        cloudEvent.DataContentType = "text/plain";
        cloudEvent.Data = "hello";

        var act = () => Mapper.ToEnvelope(cloudEvent);

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be("datacontenttype");
    }

    [Fact]
    public void ToEnvelope_WhenDataIsBinary_RejectsIt()
    {
        var cloudEvent = ValidCloudEvent();
        cloudEvent.Data = new byte[] { 1, 2, 3 };

        var act = () => Mapper.ToEnvelope(cloudEvent);

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be("data");
    }

    [Fact]
    public void ToCloudEvent_WhenIdIsBlank_RejectsIt()
    {
        var envelope = new DomainEventEnvelope("order", "42", "created");

        var act = () => Mapper.ToCloudEvent(envelope, " ");

        act.Should().Throw<EmptyArgumentException>();
    }

    [Fact]
    public void ToCloudEvent_WhenIdentityIsNotRetained_UsesNewVersionSevenIdAndDefaultSource()
    {
        var envelope = new DomainEventEnvelope("order", "42", "created");

        var first = Mapper.ToCloudEvent(envelope);
        var second = Mapper.ToCloudEvent(envelope);

        Guid.Parse(first.Id!).Version.Should().Be(7);
        Guid.Parse(second.Id!).Version.Should().Be(7);
        first.Id.Should().NotBe(second.Id);
        first.Source.Should().Be(Source);
    }

    [Fact]
    public void ToCloudEvent_WhenIdentityIsRetained_UsesItUnlessExplicitlyOverridden()
    {
        var envelope = Mapper.ToEnvelope(ValidCloudEvent());
        var otherSource = new Uri("/other", UriKind.Relative);
        var mapper = new CloudEventEnvelopeMapper(otherSource);

        var retained = mapper.ToCloudEvent(envelope);
        var overridden = mapper.ToCloudEvent(envelope, "other-event", otherSource);

        retained.Id.Should().Be("1");
        retained.Source.Should().Be(Source);
        overridden.Id.Should().Be("other-event");
        overridden.Source.Should().Be(otherSource);
    }

    [Fact]
    public void ToCloudEvent_WhenNoSourceIsAvailable_RejectsIt()
    {
        var mapper = new CloudEventEnvelopeMapper();
        var act = () => mapper.ToCloudEvent(new DomainEventEnvelope("order", "42", "created"));

        act.Should().Throw<EmptyArgumentException>();
        mapper.ToCloudEvent(new DomainEventEnvelope("order", "42", "created"), source: Source).Source.Should().Be(Source);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("source")]
    [InlineData("specversion")]
    [InlineData("datacontenttype")]
    [InlineData("dataschema")]
    [InlineData("data")]
    [InlineData("data_base64")]
    [InlineData("cloudevents.other")]
    [InlineData("bad-name")]
    [InlineData("Uppercase")]
    [InlineData("")]
    [InlineData(CloudEventEnvelopeMapper.AggregateTypeAttribute)]
    [InlineData(CloudEventEnvelopeMapper.PersistedIdAttribute)]
    public void ToCloudEvent_WhenCustomAttributeCollidesOrIsMalformed_RejectsIt(string name)
    {
        var envelope = new DomainEventEnvelope("order", "42", "created",
            customAttributes: new Dictionary<string, string> { [name] = "value" });

        var act = () => Mapper.ToCloudEvent(envelope);

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be(name);
    }

    [Theory]
    [InlineData("id", "storageid", "id")]
    [InlineData("kind", "kind", "kind")]
    [InlineData("bad-name", "storageid", "bad-name")]
    public void ToCloudEvent_WhenMappingNamesAreInvalidOrColliding_RejectsThem(string aggregateName, string persistedIdName, string collision)
    {
        var mapper = new CloudEventEnvelopeMapper(Source, aggregateName, persistedIdName);

        var act = () => mapper.ToCloudEvent(new DomainEventEnvelope("order", "42", "created"));

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be(collision);
    }

    [Fact]
    public void ToEnvelope_WhenMappingNamesAreConfigured_ConsumesOnlyConfiguredExtensions()
    {
        var mapper = new CloudEventEnvelopeMapper(Source, "kind", "storageid");
        var envelope = new PersistedDomainEvent(42, "order", "42", "created");

        var cloudEvent = mapper.ToCloudEvent(envelope);
        var restored = mapper.ToEnvelope(cloudEvent);

        cloudEvent["kind"].Should().Be("order");
        cloudEvent["storageid"].Should().Be("42");
        cloudEvent[CloudEventEnvelopeMapper.AggregateTypeAttribute].Should().BeNull();
        restored.Should().BeOfType<PersistedDomainEvent>().Which.Id.Should().Be(42);
        restored.CustomAttributes.Should().NotContainKey("kind").And.NotContainKey("storageid");
    }

    [Theory]
    [InlineData(CloudEventEnvelopeMapper.SpecVersionContextKey, "2.0", "specversion")]
    [InlineData(CloudEventEnvelopeMapper.DataSchemaContextKey, "relative/path", CloudEventEnvelopeMapper.DataSchemaContextKey)]
    [InlineData(CloudEventEnvelopeMapper.DataContentTypeContextKey, "malformed", "datacontenttype")]
    [InlineData(CloudEventEnvelopeMapper.IdContextKey, " ", CloudEventEnvelopeMapper.IdContextKey)]
    public void ToCloudEvent_WhenRetainedContextIsMalformed_RejectsIt(string key, string value, string attribute)
    {
        var envelope = new DomainEventEnvelope("order", "42", "created",
            customAttributes: new Dictionary<string, string> { [key] = value });

        var act = () => Mapper.ToCloudEvent(envelope);

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be(attribute);
    }

    [Fact]
    public void ToEnvelopeAndBack_WhenContextKeysAreOverridden_RetainsMetadataWithoutEmittingContextKeys()
    {
        var mapper = new CustomContextMapper();
        var original = ValidCloudEvent();
        original.DataSchema = new Uri("https://example.test/order.json");
        original.DataContentType = "application/vnd.order+json";
        original["traceparent"] = "trace-value";

        var envelope = mapper.ToEnvelope(original);
        var remapped = mapper.ToCloudEvent(envelope);

        envelope.CustomAttributes.Should().BeEquivalentTo(new Dictionary<string, string> {
            ["meta.id"] = "1",
            ["meta.source"] = "/orders",
            ["meta.version"] = "1.0",
            ["meta.contenttype"] = "application/vnd.order+json",
            ["metaschema"] = "https://example.test/order.json",
            ["traceparent"] = "trace-value"
        });
        remapped.Id.Should().Be(original.Id);
        remapped.Source.Should().Be(original.Source);
        remapped.SpecVersion.Should().Be(original.SpecVersion);
        remapped.DataContentType.Should().Be(original.DataContentType);
        remapped.DataSchema.Should().Be(original.DataSchema);
        remapped["traceparent"].Should().Be("trace-value");
        remapped.ExtensionAttributes.Select(attribute => attribute.Name).Should().NotContain("metaschema");
    }

    [Fact]
    public void ToCloudEvent_WhenOverriddenVersionKeyContainsUnsupportedVersion_RejectsIt()
    {
        var envelope = new DomainEventEnvelope("order", "42", "created",
            customAttributes: new Dictionary<string, string> { ["meta.version"] = "2.0" });
        var mapper = new CustomContextMapper();

        var act = () => mapper.ToCloudEvent(envelope, source: Source);

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be("specversion");
    }

    [Fact]
    public void ToEnvelope_WhenExtensionMatchesOverriddenContextKey_RejectsItEvenIfContextIsAbsent()
    {
        var cloudEvent = ValidCloudEvent();
        cloudEvent["metaschema"] = "not the schema";
        var mapper = new CustomContextMapper();

        var act = () => mapper.ToEnvelope(cloudEvent);

        act.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be("metaschema");
    }

    [Theory]
    [InlineData("meta.id")]
    [InlineData("source")]
    [InlineData(CloudEventEnvelopeMapper.AggregateTypeAttribute)]
    [InlineData(CloudEventEnvelopeMapper.PersistedIdAttribute)]
    [InlineData(" ")]
    public void Mapping_WhenOverriddenContextKeyIsInvalidOrColliding_RejectsItInBothDirections(string schemaKey)
    {
        var mapper = new CustomContextMapper(schemaKey);
        var fromCloudEvent = () => mapper.ToEnvelope(ValidCloudEvent());
        var toCloudEvent = () => mapper.ToCloudEvent(new DomainEventEnvelope("order", "42", "created"), source: Source);

        fromCloudEvent.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be(schemaKey);
        toCloudEvent.Should().Throw<InvalidCloudEventMappingException>().Which.Attribute.Should().Be(schemaKey);
    }

    private static CloudEvent ValidCloudEvent() => Mapper.ToCloudEvent(
        new DomainEventEnvelope("order", "42", "created", time: Moment), "1");

    private sealed record OrderData(int Total);

    private sealed class CustomContextMapper(string schemaKey = "metaschema") : CloudEventEnvelopeMapper
    {
        protected override string IdContextAttributeName => "meta.id";
        protected override string SourceContextAttributeName => "meta.source";
        protected override string SpecVersionContextAttributeName => "meta.version";
        protected override string DataContentTypeContextAttributeName => "meta.contenttype";
        protected override string DataSchemaContextAttributeName => schemaKey;
    }
}
