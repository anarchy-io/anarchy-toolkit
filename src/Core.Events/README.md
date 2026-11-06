# Anarchy.Toolkit.Core.Events

In-process publish/subscribe and building blocks for a generic domain-event pipeline.

```bash
dotnet add package Anarchy.Toolkit.Core.Events
```

- Implement `ILocalEvent` and `ILocalEventHandler<T>` and register the local bus with `services.AddLocalEventBus()`.
- Build domain-event processing around `EventEnvelopeBase`, `Event`, `PersistedEvent`, the processor, handler bus and `EventEnvelopeParser`.
- The local bus is in-process; use a message-broker implementation for external delivery.

See the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme) and [architecture guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/architecture.md).
