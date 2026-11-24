# Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** Infrastructure · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Maps Anarchy event envelopes and broker-neutral message bodies to and from [CloudEvents 1.0](https://cloudevents.io/) using the CloudEvents .NET SDK. The package is independent of the outbox and of any transport.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents
```

## When to use it

Use this package when producers and consumers agree on CloudEvents representation for domain events. CloudEvents standardizes event representation and metadata, **not delivery guarantees**: configure durability, retries, ordering, idempotency and deduplication separately in your transport or outbox.

## Mapping

| Anarchy envelope or caller | CloudEvents 1.0 |
|---|---|
| Explicit ID, retained `cloudevents.id`, or a new UUIDv7 | `id` |
| Explicit producer URI reference, retained `cloudevents.source`, or the mapper's default source | `source` |
| `cloudevents.specversion`, defaults to `1.0` | `specversion` |
| `Type`, `Subject`, `Time` | `type`, `subject`, `time` |
| `Data` | JSON `data` |
| Explicit value or retained `cloudevents.datacontenttype`, defaults to `application/json` | `datacontenttype` |
| Explicit absolute URI or retained `cloudevents.dataschema` | `dataschema` |
| `AggregateType` | `anarchyaggregatetype` string extension by default |
| `IPersistedEventEnvelope.Id` | optional `anarchypersistedid` string extension by default; invariant signed 64-bit decimal |
| Other `CustomAttributes` | CloudEvent extensions |

The pair `id` and `source` identifies the CloudEvent. Calls without an explicit or retained ID generate a fresh UUIDv7 **each time**; generation does not mutate the input envelope. For retries, pass the same ID and source or retain the event (or its mapped-back envelope). A mapper's default source is only a fallback. Storage IDs, broker message IDs, and broker deduplication IDs are separate and are never inferred from event identity.

On input, `id`, `source`, `type`, `subject`, `time`, and the aggregate-type extension are required. If the persisted-ID extension is present and valid, the result is `PersistedDomainEvent`; otherwise it is `DomainEventEnvelope`. The mapper retains CloudEvent context under five envelope-only keys. The defaults are the `cloudevents.*` keys in the table; their public constants end in `ContextKey`. They are not wire extensions. Active context keys must be nonblank and distinct from one another, the specification names, and the active aggregate-type and persisted-ID extension names. An inbound extension cannot use an active context key, even when the corresponding context field is absent.

The domain envelope's `CustomAttributes` dictionary can hold arbitrary application keys, but the default mapper only accepts active context keys and valid CloudEvents extension names (lowercase ASCII letters or digits, with non-null string values). It rejects invalid names and collisions with core names, `data`, `data_base64`, or the active aggregate-type or persisted-ID extension name rather than silently dropping attributes. A derived mapper can explicitly translate or filter application metadata in `MapCustomAttributesToCloudEvent` and `MapCustomAttributesFromCloudEvent`. Wire bodies with duplicate JSON property names are rejected too.

String extensions round-trip as strings. For SDK-typed extensions, the envelope stores the canonical string value (for example, `true`, `12`, or base64 for binary). Remapping returns those values as **string extensions**, not the original typed extensions or their SDK definitions. Direct broker body conversion preserves JSON boolean and integer extension types; a JSON string does not identify a URI, timestamp, or binary extension type on its own. Keep the original `CloudEvent` or message when those distinctions or exact wire bytes matter.

Envelope mapping supports `application/json` and `+json` data, not non-JSON content types or top-level binary data. Missing or JSON-null data becomes null. When absent, the data content type is treated as JSON and may be emitted explicitly on remapping. The SDK formatter uses Newtonsoft.Json. Mapped data is a `JToken`; use `CastData<T>()` or work with the JSON tokens directly. JSON string values are not parsed twice. CLR types are not encoded in the wire format, and the domain envelope's type, subject, and aggregate type are trimmed on construction.

## Broker-neutral structured JSON

```csharp
using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes;

ICloudEventEnvelopeMapper mapper = new CloudEventEnvelopeMapper(new Uri("/orders", UriKind.Relative));
var envelope = new DomainEventEnvelope("order", "order-42", "order-created", new { Total = 10 });
var cloudEvent = mapper.ToCloudEvent(envelope, "event-123");
var outgoing = mapper.ToOutboundMessage(cloudEvent, messageId: "publish-1", destination: "orders-topic");

// After receipt, an adapter provides the actual broker context on an InboundMessage.
var incoming = mapper.ToInboundMessage(mapper.ToCloudEvent(outgoing), messageId: "delivery-1",
    brokerSource: "orders-queue", brokerReference: "receipt-1");
var restored = mapper.ToEnvelope(mapper.ToCloudEvent(incoming));
var forwarded = mapper.ToOutboundMessage(mapper.ToCloudEvent(restored), "publish-2", "archive-topic",
    partitionKey: incoming.PartitionKey, attributes: incoming.Attributes);
```

The broker body is `application/cloudevents+json` (UTF-8). This is separate from the event's `datacontenttype`. The message models have no dedicated media-type field; the mapper does not inject a broker attribute for it. Reading either message type decodes only its body. Broker metadata stays on the original message and is passed explicitly when building a new message. For example, a broker `source` attribute or correlation ID can differ from the CloudEvent's source or correlation extension without collision. Binary-mode bindings, publishing, acknowledgement, and broker-specific content-type headers remain the transport's responsibility.

## Dependency injection and customization

Use the service collection extension or `UseModule(new ModuleCloudEventEnvelopeMapper(source), configuration, environment)`. Both register `ICloudEventEnvelopeMapper` as a singleton and preserve a preexisting registration. They also accept a factory for a custom implementation. Derived singleton mappers should be thread-safe.

```csharp
using Anarchy.Toolkit.Core.Events.Envelopes;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Envelopes;
using Anarchy.Toolkit.Infrastructure.Messaging.CloudEvents.Extension.Microsoft.DependencyInjection;
using CloudNative.CloudEvents;

services.AddCloudEventEnvelopeMapper(_ => new OrderMapper(new Uri("/orders", UriKind.Relative)));

public sealed class OrderMapper(Uri source) : CloudEventEnvelopeMapper(source)
{
    protected override string AggregateTypeAttributeName => "aggregatekind";

    protected override string MapTypeToCloudEvent(IEventEnvelope envelope) => $"orders.{envelope.Type}";

    protected override string MapTypeFromCloudEvent(CloudEvent cloudEvent)
    {
        var type = base.MapTypeFromCloudEvent(cloudEvent);
        return type.StartsWith("orders.", StringComparison.Ordinal) ? type["orders.".Length..] : type;
    }
}
```

Each CloudEvent property and the custom extensions have a protected virtual hook in each direction (`Map*ToCloudEvent` and `Map*FromCloudEvent`). Configure the two Anarchy extension names in the constructor, or override `AggregateTypeAttributeName` and `PersistedIdAttributeName`. Omitting names uses the constants in the mapping table; when custom names are configured, only those names are used for that mapping, in both directions.

Override `IdContextAttributeName`, `SourceContextAttributeName`, `SpecVersionContextAttributeName`, `DataContentTypeContextAttributeName`, or `DataSchemaContextAttributeName` to rename the corresponding envelope-only keys. Return stable names: the mapper uses the same keys to read and write context, and does not also recognize a default key when it has been overridden. CloudEvents wire field names are unchanged.

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
