# Observability

This guide describes how applications built on the toolkit write logs and emit metrics through `Observer`, and where that telemetry is sent at runtime.

> **A guide, not a rule.** This page explains how the platform works. The conventions it mentions are recommendations: see the [Observability convention](../conventions/observability.md) for the rules, why they exist and how to opt out.
{: .notice--info}

## The building blocks

| Package | What it provides |
|---|---|
| `Anarchy.Toolkit.Core.Observability` | `Observer` facade exposing `Logger` ([Serilog](https://serilog.net/) `ILogger`), `MeasurerBase` (metrics, ambient tags via `MeasurerBase.BeginScope`) and `LogContext` (ambient log properties), with call-site aware logging + metrics, `Tags`, `CallSiteEntity` + `CalledFrom()`/`ParentCaller()`, all under `Service.Observability`. |
| `Anarchy.Toolkit.Infrastructure.Observability.Serilog` | Standard log properties with overridable names (`WithTeam`, `WithAppGroup`, `WithCostCenter`, `WithCostCenterFromEnvironment`, `WithLogRetention`), `WithAwsEc2InstanceId` and the `Ec2CloudWatch(logGroup)` sink. |
| `Anarchy.Toolkit.Infrastructure.CorrelationId` | `CorrelationContext`, `UseCorrelationId()` middleware (headers from `CorrelationIdOptions`, `X-Correlation-Id`/`X-Attempt-Id` by default), `WithCorrelationId()` log enricher (property names overridable). |
| `Anarchy.Toolkit.Configuration` | `ModuleObservability`, which registers the `Observer` built by an `ObserverFactory` (vendor-neutral by default, overridable step by step). |
| `Anarchy.Toolkit.Testing` | `InMemoryObserver`, with in-memory log and metric sinks plus assertions, for tests. |

## Call sites

`Observer` attaches the calling class and method to every log line and metric, so call sites carry the context:

```csharp
var caller = this.CalledFrom();

observer.LogInformation(caller, "Processing user {UserId}", userId);
observer.ExecutionIncrementCount(caller, 1, Tags.Success);
```

If `this.CalledFrom()` is not available (for example in a static method), build the call site explicitly:

```csharp
var caller = new CallSiteEntity("UserMapper", "Map");
observer.LogWarning(caller, "Falling back to default role for {UserId}", userId);
```

`this.ParentCaller()` returns the call site one frame up. The database policies use it to tag their duration metric with the repository method that triggered the query.

How to word messages, name metrics and choose tags is in the [Observability convention](../conventions/observability.md).

## Runtime collection

`ModuleObservability` (registered through `ModuleLibConfiguration`) asks an `ObserverFactory` for the `Observer` singleton. It reads an optional `Observability` configuration section:

```json
{
  "Observability": {
    "AppNamePrefix": "UserManagement."
  }
}
```

`AppNamePrefix` is prepended to the application name when missing; the result becomes the `app_name` log property and, snake-cased, the metric prefix.

### What the default factory does

Out of the box the toolkit is vendor-neutral:

- **Locally**: readable console logs at `Debug` level; metrics are printed by `ConsoleMeasurer`.
- **Hosted (Lambda, EC2 or containers)**: JSON console logs with the log context, correlation ids, structured exceptions and `app_name` (plus the AWS request context on Lambda). Metrics are written as structured log events by `LoggerMeasurer`, so nothing is lost before a metrics backend is plugged in.

### Plugging in an organization's setup

Derive from `ObserverFactory` and override only the steps that differ, then hand it to `ModuleObservability`:

| Override | To change |
|---|---|
| `CorrelationIdPropertyName` / `AttemptIdPropertyName` | the log property names of the correlation ids |
| `Enrich` | the hosted log properties (team, cost center, instance IDs, …) |
| `WriteTo` | the hosted sinks (e.g. `Ec2CloudWatch(logGroup)`) |
| `CreateMeasurer` | the metrics backend (e.g. a `PrometheusMeasurer` that ships metrics to your provider) |
| `CreateBaseLoggerConfiguration`, `CreateLocal`, `CreateHosted`, `Create` | everything else |

```csharp
public sealed class CustomObserverFactory : ObserverFactory
{
    protected override LoggerConfiguration Enrich(LoggerConfiguration configuration, ObserverContext context) =>
        base.Enrich(configuration, context).Enrich.WithTeam("identity").Enrich.WithCostCenter("platform");

    protected override MeasurerBase CreateMeasurer(ObserverContext context, ILogger logger) =>
        new PrometheusMeasurer();
}

services.UseModule(
    new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(
        new ModuleObservability(new CustomObserverFactory(), config => new CorrelationIdOptions { CorrelationIdHeaderName = "X-Request-Id" })),
    configuration,
    environment);
```

A shared factory can, for example, add a metrics backend, CloudWatch logging on EC2, the standard log properties under the organization's own names and custom correlation headers, without changes to the toolkit.

## Testing telemetry

`TestCaseBase.FixtureObserver` is an `InMemoryObserver`, so logs and metrics can be asserted when observability *is* the behavior under test:

```csharp
FixtureObserver.SinkLogs.Should().HaveMessage("Created user {UserId}");
FixtureObserver.InMemoryMeasurer.Should().HaveCounter("user_service.create.execution_count");
```

Avoid asserting incidental logs or metrics in ordinary business-rule tests; assert the business result first. See [Testing Conventions](../conventions/testing.md).
