# Anarchy.Toolkit.Infrastructure.Messaging.Aws

AWS SNS and SQS publishers implementing the toolkit's message-broker abstractions.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Messaging.Aws
```

- Register publishers with `AddSnsMessageBroker` or `AddSqsMessageBroker` and `AwsMessagingOptions`, or use `ModuleSnsMessageBroker` and `ModuleSqsMessageBroker` with configuration-based options factories.
- The package also provides shared publisher and request-builder bases, inbound mapping and compression.
- For integration tests, see [Testing.Aws](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Aws) and its LocalStack helpers.

## When to use it

Use this package at the composition root when application code depends on `IMessageBrokerPublisher` and the deployment uses SNS topics or SQS queues. It implements publishing and inbound message mapping; the application remains responsible for creating destinations, permissions and message consumption.

## How to use it

Register an `Observer` and the required broker adapters. The example uses explicit options; obtain real account and region values from your host configuration:

```csharp
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.Microsoft.DependencyInjection;

var options = new AwsMessagingOptions("eu-west-1", "123456789012");
services.AddSnsMessageBroker(options);
services.AddSqsMessageBroker(options);
```

Alternatively, register the modules with options factories using `UseModule`. Resolve the keyed `topic` or `queue` publisher and build messages with `OutboundMessageBuilder` from the abstractions package. Configure AWS credentials and permissions for the actual destinations, and plan for broker size limits, batch failures and duplicate delivery.

See the [registration example](https://github.com/anarchy-io/anarchy-toolkit#consuming-a-package) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.3

- build: migrate local automation to NUKE

### 1.0.2

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced SNS and SQS publishers, request builders, optional payload compression, inbound mapping and dependency-injection modules.
