# Anarchy.Toolkit.Infrastructure.Messaging.Aws

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

AWS SNS and SQS publishers implementing the toolkit's message-broker abstractions.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Messaging.Aws
```

- Register publishers with `AddSnsMessageBroker` or `AddSqsMessageBroker` and `AwsMessagingOptions`, or use `ModuleSnsMessageBroker` and `ModuleSqsMessageBroker` with configuration-based options factories.
- The package also provides shared publisher and request-builder bases, inbound mapping and compression.
- `InboundMessage.BeginCorrelationScope(logger)` is the consumer entry point for correlation: it reads the correlation and attempt IDs from the mapped message's string attributes and opens the provider-neutral scope from `Infrastructure.CorrelationId`.
- For integration tests, see [Testing.Aws](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Aws) and its LocalStack helpers.

## When to use it

Use this package at the composition root when application code depends on `IMessageBrokerPublisher` and the deployment uses SNS topics or SQS queues. It implements publishing and inbound message mapping; the application remains responsible for creating destinations, permissions and message consumption.

## How to use it

Register `AppTelemetry` and the required broker adapters. The example uses explicit options; obtain real account and region values from your host configuration:

```csharp
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.Microsoft.DependencyInjection;

var options = new AwsMessagingOptions("eu-west-1", "123456789012");
services.AddSnsMessageBroker(options);
services.AddSqsMessageBroker(options);
```

Alternatively, register the modules with options factories using `UseModule`. Resolve the keyed `topic` or `queue` publisher and build messages with `OutboundMessageBuilder` from the abstractions package. Configure AWS credentials and permissions for the actual destinations, and plan for broker size limits, batch failures and duplicate delivery.

### Consuming messages with correlation

The package maps inbound messages (`InboundMessageMapper.FromSqs(sqsEvent)`) but does not ship a consumer loop, so nothing propagates correlation IDs on its own: your handler opts in by opening the scope around each message.

```csharp
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.MessageBroker.Aws;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Helper.MessageBroker;

foreach (var message in InboundMessageMapper.FromSqs(sqsEvent))
{
    using (message.BeginCorrelationScope(logger))
        await processor.HandleAsync(message, cancellationToken);
}
```

The scope sets the async-flowing `CorrelationContext`, opens a standard `ILogger` scope with the correlation and attempt properties (so every logging provider sees them) and tags the current `Activity`. The attribute names default to `CorrelationId` and `AttemptId`; pass other names when the publisher uses different ones. A missing attribute leaves that ID empty rather than generating one. Publishers do not add the attributes: copy `CorrelationContext.CorrelationId` into the outbound message attributes yourself if downstream consumers need it.

See the [registration example](https://anarchy-io.github.io/anarchy-toolkit/getting-started/consuming-packages.html) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
