# Anarchy.Toolkit.Core.Messaging.Abstractions

Contracts and entities for publishing and receiving broker messages without coupling application code to a broker.

```bash
dotnet add package Anarchy.Toolkit.Core.Messaging.Abstractions
```

- Depend on `IMessageBrokerPublisher` for outbound publishing.
- Model inbound and outbound messages and construct outbound messages with `OutboundMessageBuilder`.
- Supply a concrete broker adapter, such as [Infrastructure.Messaging.Aws](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Messaging.Aws), at the composition root.

See the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
