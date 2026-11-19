# Anarchy.Toolkit.Core.Events

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Core · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

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

Register `IAppTelemetry` and the bus, then register each handler explicitly. For example, with an application-defined `OrderPlaced : ILocalEvent` and `OrderPlacedHandler : ILocalEventHandler<OrderPlaced>`:

```csharp
using Anarchy.Toolkit.Core.Events.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Events.Service.Event.InMemory;
using Anarchy.Toolkit.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;

services.AddLogging();
services.AddMetrics();
services.AddAppTelemetry();
services.AddLocalEventBus();
services.AddScoped<ILocalEventHandler<OrderPlaced>, OrderPlacedHandler>();

// In a service that receives ILocalEventBus via DI:
await bus.PublishAsync(new OrderPlaced(), absorbErrors: false, ct: cancellationToken);
```

Each publish resolves handlers in a new DI scope and awaits them sequentially. Handler exceptions are logged and absorbed by default; set `absorbErrors: false` to propagate the first failure. No registered handlers is a no-op. For external delivery, compose this package with a broker adapter instead.

See the [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/) and [architecture guide](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html).

## Releases

### 2.0.0

- Local event bus handler failures use the injected `IAppTelemetry` instead of `Observer`.
- Domain event processing uses the same facade for event scopes, logs and metrics, removing its Serilog-based dependency.

### 1.1.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.1.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

### 1.0.6

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.0.5

- refactor(src): clean up code style across library packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced domain event envelopes, handlers and processing, the local event bus and message broker envelope parsing.
