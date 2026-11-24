using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Extension.Microsoft.DependencyInjection;
using AwesomeAssertions;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Newtonsoft.Json.Linq;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Messaging.CloudEvents.Envelopes;

public class CloudEventMapperRegistrationTest
{
    private static readonly Uri Source = new("/orders", UriKind.Relative);
    private static readonly Uri Schema = new("https://example.test/order.json");

    [Fact]
    public void AddCloudEventEnvelopeMapper_WhenDefaultSourceProvided_RegistersSingleton()
    {
        var services = new ServiceCollection().AddCloudEventEnvelopeMapper(Source);
        using var provider = services.BuildServiceProvider();

        var mapper = provider.GetRequiredService<ICloudEventEnvelopeMapper>();

        provider.GetRequiredService<ICloudEventEnvelopeMapper>().Should().BeSameAs(mapper);
        mapper.ToCloudEvent(new DomainEventEnvelope("order", "42", "created")).Source.Should().Be(Source);
    }

    [Fact]
    public void RegisterServices_WhenDerivedFactoryIsSupplied_UsesOverridesInBothDirections()
    {
        var services = new ServiceCollection().AddSingleton(Source);
        var module = new ModuleCloudEventEnvelopeMapper(provider => new CustomMapper(provider.GetRequiredService<Uri>()));
        module.RegisterServices(services, new ConfigurationBuilder().Build(), Mock.Of<IHostEnvironment>());
        services.AddCloudEventEnvelopeMapper(); // Do not replace the first registration.
        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<ICloudEventEnvelopeMapper>();
        var time = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var envelope = new PersistedDomainEvent(42, "order", "42", "created", new JValue("data"), time);

        var cloudEvent = mapper.ToCloudEvent(envelope, "event-1");
        var restored = mapper.ToEnvelope(cloudEvent);

        var customized = mapper.Should().BeOfType<CustomMapper>().Which;
        customized.MappedVersionToCloudEvent.Should().BeTrue();
        customized.MappedVersionFromCloudEvent.Should().BeTrue();
        cloudEvent.SpecVersion.Should().Be(CloudEventsSpecVersion.V1_0);
        cloudEvent.Id.Should().Be("out:event-1");
        cloudEvent.Source!.OriginalString.Should().Be("/orders/out");
        cloudEvent.Type.Should().Be("out:created");
        cloudEvent.Subject.Should().Be("out:42");
        cloudEvent.Time.Should().Be(time.AddHours(1));
        cloudEvent.DataContentType.Should().Be("application/vnd.order+json");
        cloudEvent.DataSchema.Should().Be(Schema);
        ((JValue)cloudEvent.Data!).Value.Should().Be("out:data");
        cloudEvent["kind"].Should().Be("out:order");
        cloudEvent["storageid"].Should().Be("out:42");
        cloudEvent["custom"].Should().Be("out");
        restored.Should().BeOfType<PersistedDomainEvent>().Which.Id.Should().Be(42);
        restored.Type.Should().Be("created");
        restored.Subject.Should().Be("42");
        restored.AggregateType.Should().Be("order");
        restored.Time.Should().Be(time);
        restored.CastData<string>().Should().Be("data");
        restored.CustomAttributes.Should().Contain(CloudEventEnvelopeMapper.SpecVersionContextKey, "1.0")
            .And.Contain(CloudEventEnvelopeMapper.IdContextKey, "event-1")
            .And.Contain(CloudEventEnvelopeMapper.SourceContextKey, "/orders")
            .And.Contain(CloudEventEnvelopeMapper.DataContentTypeContextKey, "application/json")
            .And.Contain(CloudEventEnvelopeMapper.DataSchemaContextKey, "https://example.test/stored.json")
            .And.Contain("custom", "in");
    }

    private sealed class CustomMapper(Uri source) : CloudEventEnvelopeMapper(source)
    {
        public bool MappedVersionToCloudEvent { get; private set; }
        public bool MappedVersionFromCloudEvent { get; private set; }

        protected override string AggregateTypeAttributeName => "kind";
        protected override string PersistedIdAttributeName => "storageid";

        protected override CloudEventsSpecVersion MapSpecVersionToCloudEvent(IEventEnvelope envelope)
        {
            MappedVersionToCloudEvent = true;
            return base.MapSpecVersionToCloudEvent(envelope);
        }

        protected override string MapIdToCloudEvent(IEventEnvelope envelope, string? id) => "out:" + base.MapIdToCloudEvent(envelope, id);
        protected override Uri MapSourceToCloudEvent(IEventEnvelope envelope, Uri? source)
            => new(base.MapSourceToCloudEvent(envelope, source).OriginalString + "/out", UriKind.Relative);
        protected override string MapTypeToCloudEvent(IEventEnvelope envelope) => "out:" + envelope.Type;
        protected override string MapSubjectToCloudEvent(IEventEnvelope envelope) => "out:" + envelope.Subject;
        protected override DateTimeOffset? MapTimeToCloudEvent(IEventEnvelope envelope) => envelope.Time.AddHours(1);
        protected override string? MapDataContentTypeToCloudEvent(IEventEnvelope envelope, string? dataContentType) => "application/vnd.order+json";
        protected override Uri? MapDataSchemaToCloudEvent(IEventEnvelope envelope, Uri? dataSchema) => Schema;
        protected override object? MapDataToCloudEvent(IEventEnvelope envelope) => new JValue("out:" + ((JValue)envelope.Data!).Value);
        protected override string MapAggregateTypeToCloudEvent(IEventEnvelope envelope) => "out:" + envelope.AggregateType;
        protected override string? MapPersistedIdToCloudEvent(IEventEnvelope envelope) => "out:" + base.MapPersistedIdToCloudEvent(envelope);
        protected override IReadOnlyDictionary<string, string> MapCustomAttributesToCloudEvent(IEventEnvelope envelope)
            => new Dictionary<string, string> { ["custom"] = "out" };

        protected override CloudEventsSpecVersion MapSpecVersionFromCloudEvent(CloudEvent cloudEvent)
        {
            MappedVersionFromCloudEvent = true;
            return base.MapSpecVersionFromCloudEvent(cloudEvent);
        }

        protected override string MapIdFromCloudEvent(CloudEvent cloudEvent) => base.MapIdFromCloudEvent(cloudEvent)[4..];
        protected override Uri MapSourceFromCloudEvent(CloudEvent cloudEvent)
            => new(base.MapSourceFromCloudEvent(cloudEvent).OriginalString[..^4], UriKind.Relative);
        protected override string MapTypeFromCloudEvent(CloudEvent cloudEvent) => base.MapTypeFromCloudEvent(cloudEvent)[4..];
        protected override string MapSubjectFromCloudEvent(CloudEvent cloudEvent) => base.MapSubjectFromCloudEvent(cloudEvent)[4..];
        protected override DateTimeOffset MapTimeFromCloudEvent(CloudEvent cloudEvent) => base.MapTimeFromCloudEvent(cloudEvent).AddHours(-1);
        protected override string? MapDataContentTypeFromCloudEvent(CloudEvent cloudEvent) => "application/json";
        protected override Uri? MapDataSchemaFromCloudEvent(CloudEvent cloudEvent) => new("https://example.test/stored.json");
        protected override object? MapDataFromCloudEvent(CloudEvent cloudEvent) => new JValue(((string)((JValue)cloudEvent.Data!).Value!)[4..]);
        protected override string MapAggregateTypeFromCloudEvent(CloudEvent cloudEvent, string attributeName)
            => base.MapAggregateTypeFromCloudEvent(cloudEvent, attributeName)[4..];
        protected override long? MapPersistedIdFromCloudEvent(CloudEvent cloudEvent, string attributeName)
            => long.Parse(((string)cloudEvent[attributeName]!)[4..], System.Globalization.CultureInfo.InvariantCulture);
        protected override IReadOnlyDictionary<string, string> MapCustomAttributesFromCloudEvent(
            CloudEvent cloudEvent, string aggregateTypeName, string persistedIdName) => new Dictionary<string, string> { ["custom"] = "in" };
    }
}
