# Anarchy.Toolkit.Infrastructure.Messaging.Aws

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

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

Register `IAppTelemetry` and the required broker adapters. The example uses explicit options; obtain real account and region values from your host configuration:

```csharp
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Entity.Configuration.Amazon;
using Anarchy.Toolkit.Infrastructure.Messaging.Aws.Extension.Microsoft.DependencyInjection;

var options = new AwsMessagingOptions("eu-west-1", "123456789012");
services.AddSnsMessageBroker(options);
services.AddSqsMessageBroker(options);
```

Alternatively, register the modules with options factories using `UseModule`. Resolve the keyed `topic` or `queue` publisher and build messages with `OutboundMessageBuilder` from the abstractions package. Configure AWS credentials and permissions for the actual destinations, and plan for broker size limits, batch failures and duplicate delivery.

See the [registration example](https://anarchy-io.github.io/anarchy-toolkit/getting-started/consuming-packages.html) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

### 2.0.0

- Route publisher logs and metrics through the host's `IAppTelemetry` instead of `Observer`.

### 1.1.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.1.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

### 1.0.7

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.0.6

- refactor(src): clean up code style across library packages
- chore(deps): update AWS SDK, Lambda ASP.NET Core server and MySqlConnector packages

### 1.0.5

- chore(deps): update AWS SDK packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced SNS and SQS publishers, request builders, optional payload compression, inbound mapping and dependency-injection modules.
