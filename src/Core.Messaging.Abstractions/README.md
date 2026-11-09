# Anarchy.Toolkit.Core.Messaging.Abstractions

Contracts and entities for publishing and receiving broker messages without coupling application code to a broker.

```bash
dotnet add package Anarchy.Toolkit.Core.Messaging.Abstractions
```

- Depend on `IMessageBrokerPublisher` for outbound publishing.
- Model inbound and outbound messages and construct outbound messages with `OutboundMessageBuilder`.
- Supply a concrete broker adapter, such as [Infrastructure.Messaging.Aws](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Messaging.Aws), at the composition root.

## When to use it

Reference these contracts in application code that publishes messages but should not know whether SNS, SQS or another broker sends them. The package defines the message shape and publishing interface; it does not connect to a broker or consume messages on its own.

## How to use it

Create an outbound message from an already serialized body and pass it to an injected publisher. The destination and optional source are application-defined names:

```csharp
using Anarchy.Toolkit.Core.Messaging.Abstractions.Service.MessageBroker;

var builder = new OutboundMessageBuilder("orders", source: "checkout");
var message = builder.Build("{\"orderId\":123}");
await publisher.PublishAsync(message, cancellationToken);
```

Register a concrete `IMessageBrokerPublisher` in the host and resolve the appropriate keyed publisher when using the AWS adapters. The builder also accepts partition keys and attributes; enum destinations are resolved through their descriptions. Batch publishing can report per-chunk outcomes and may throw if any chunk fails, so handle partial success deliberately.

See the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.3

- build: migrate local automation to NUKE

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced transport-agnostic message entities, outbound message building, publisher contracts and exceptions.
