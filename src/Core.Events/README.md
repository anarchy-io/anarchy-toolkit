# Anarchy.Toolkit.Core.Events

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Core · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

In-process publish/subscribe and building blocks for a generic domain-event pipeline.

```bash
dotnet add package Anarchy.Toolkit.Core.Events
```

- Implement `ILocalEvent` and `ILocalEventHandler<T>` and register the local bus with `services.AddLocalEventBus()`.
- Build domain-event processing around `EventEnvelopeBase`, `DomainEventEnvelope`, `PersistedDomainEvent`, `DomainEventProcessorBase<T>`, `DomainEventHandlerDispatcher<T>` and `EventEnvelopeParser`. Types live in the `Envelopes`, `Domain` and `Local` namespaces.
- The local bus is in-process; use a message-broker implementation for external delivery.

## When to use it

Use the local bus to notify other components within the same process, or use the domain-event types to build an event processing pipeline. The local bus is not a durable queue: events are lost if the process stops, and it does not deliver across service instances.

## How to use it

Register `AppTelemetry` and the bus, then register each handler explicitly. For example, with an application-defined `OrderPlaced : ILocalEvent` and `OrderPlacedHandler : ILocalEventHandler<OrderPlaced>`:

```csharp
using Anarchy.Toolkit.Core.Events.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Events.Local;
using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;

services.AddCoreTelemetry();
services.AddLocalEventBus();
services.AddScoped<ILocalEventHandler<OrderPlaced>, OrderPlacedHandler>();

// In a service that receives ILocalEventBus via DI:
await bus.PublishAsync(new OrderPlaced(), absorbErrors: false, ct: cancellationToken);
```

Each publish resolves handlers in a new DI scope and awaits them sequentially. Handler exceptions are logged and absorbed by default; set `absorbErrors: false` to propagate the first failure. No registered handlers is a no-op. For external delivery, compose this package with a broker adapter instead.

### Publish an event envelope through a broker

```csharp
using Anarchy.Toolkit.Core.Events.Envelopes;

var envelope = new DomainEventEnvelope("order", orderId, "order-created", data: new { Total = 10m });
var outbound = EventEnvelopeMessageMapper.ToOutboundMessage(envelope, "orders", publicationId);
await publisher.PublishAsync(outbound);

// On the consuming side, given the inbound message from a broker adapter:
var received = EventEnvelopeMessageMapper.ToEnvelope<DomainEventEnvelope>(inbound);
```

`ToInboundMessage` also creates an inbound message from an envelope when the broker source, message ID and delivery
reference are known. `ToEnvelope<T>` accepts either inbound or outbound messages.

Reuse the same publication ID on retries; by default it is also the message's deduplication ID. The mapper defaults the
partition key to the event's `Subject`. Both defaults can be overridden. A `PersistedDomainEvent.Id` is a storage ID,
not a broker message ID or CloudEvent ID.

The body contains the complete envelope, including its `CustomAttributes`. For broker filtering, attributes also
project `AggregateType`, `Subject` and `Type`. These projections take precedence over explicit message attributes,
which in turn override any colliding custom attributes in the broker metadata only. The receiver reads event metadata
from the body, not the broker attributes, source, destination or delivery identity. Broker locations do not identify
the event producer in other formats such as CloudEvents.

See the [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/) and [architecture guide](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
