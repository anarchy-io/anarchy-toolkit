# Anarchy.Toolkit.Core.Events

In-process publish/subscribe and building blocks for a generic domain-event pipeline.

```bash
dotnet add package Anarchy.Toolkit.Core.Events
```

- Implement `ILocalEvent` and `ILocalEventHandler<T>` and register the local bus with `services.AddLocalEventBus()`.
- Build domain-event processing around `EventEnvelopeBase`, `Event`, `PersistedEvent`, the processor, handler bus and `EventEnvelopeParser`.
- The local bus is in-process; use a message-broker implementation for external delivery.

## When to use it

Use the local bus to notify other components within the same process, or use the domain-event types to build an event processing pipeline. The local bus is not a durable queue: events are lost if the process stops, and it does not deliver across service instances.

## How to use it

Register an `Observer` and the bus, then register each handler explicitly. For example, with an application-defined `OrderPlaced : ILocalEvent` and `OrderPlacedHandler : ILocalEventHandler<OrderPlaced>`:

```csharp
using Anarchy.Toolkit.Core.Events.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Events.Service.Event.InMemory;
using Microsoft.Extensions.DependencyInjection;

services.AddLocalEventBus();
services.AddScoped<ILocalEventHandler<OrderPlaced>, OrderPlacedHandler>();

// In a service that receives ILocalEventBus via DI:
await bus.PublishAsync(new OrderPlaced(), absorbErrors: false, ct: cancellationToken);
```

Each publish resolves handlers in a new DI scope and awaits them sequentially. Handler exceptions are logged and absorbed by default; set `absorbErrors: false` to propagate the first failure. No registered handlers is a no-op. For external delivery, compose this package with a broker adapter instead.

See the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme) and [architecture guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/architecture.md).

## Releases

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced domain event envelopes, handlers and processing, the local event bus and message broker envelope parsing.
