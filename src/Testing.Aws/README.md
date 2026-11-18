# Anarchy.Toolkit.Testing.Aws

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Testing kit · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

AWS messaging helpers for SNS/SQS integration tests, typically against LocalStack.

```bash
dotnet add package Anarchy.Toolkit.Testing.Aws
```

- Use `SqsHelper` and `AwsMessageBrokerHelper` to work with test queues and messaging resources.
- `SqsMessageFixture` builds SQS messages, including gzip/zlib-compressed payloads.
- Supply a suitable AWS or LocalStack endpoint and resources for your integration tests.

## When to use it

Use this test-only package to exercise SNS/SQS publishing or message processing against a controlled AWS-compatible environment, or to construct SQS event fixtures without a live broker. It does not start LocalStack or provision infrastructure automatically for the whole test suite.

## How to use it

1. Start a dedicated LocalStack instance or configure a separate test AWS account and supply an application settings object implementing `IAwsAppSettings`.
2. Construct `AwsMessageBrokerHelper` with those settings. Use its queue/topic preparation methods and, when needed, subscribe a queue to a topic before exercising the code under test.
3. Receive and assert messages with the helper; use `SqsMessageFixture` when testing a Lambda handler with a constructed `SQSEvent`, including compressed bodies.
4. Dispose the helper and clean up test resources. Use unique names or isolate tests that purge queues, and never point destructive cleanup at production resources.

The generic `SqsHelper<TAppSettings>` can use the test settings of your settings type; see the [testing guide](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html) for details. `PrepareTopicAsync` deletes and recreates an existing topic, so isolate resource names carefully.

See [Infrastructure.Messaging.Aws](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Messaging.Aws), the [testing conventions](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html) and the [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

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

- Introduced AWS messaging test helpers, SQS helpers and an SQS message fixture.
