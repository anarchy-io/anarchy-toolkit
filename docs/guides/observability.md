# Observability

This is the observability guide for applications built on the toolkit: how to log and emit metrics through `Observer`, and where that telemetry ends up at runtime. 🔭

## The building blocks

| Package | What it provides |
|---|---|
| `Anarchy.Toolkit.Core.Observability` | `Observer` facade exposing `Logger` (Serilog `ILogger`), `MeasurerBase` (metrics, ambient tags via `MeasurerBase.BeginScope`) and `LogContext` (ambient log properties), with call-site aware logging + metrics, `Tags`, `CallSiteEntity` + `CalledFrom()`/`ParentCaller()`, all under `Service.Observability`. |
| `Anarchy.Toolkit.Infrastructure.Observability.Serilog` | Standard log properties with overridable names (`WithTeam`, `WithAppGroup`, `WithCostCenter`, `WithCostCenterFromEnvironment`, `WithLogRetention`), `WithAwsEc2InstanceId` and the `Ec2CloudWatch(logGroup)` sink. |
| `Anarchy.Toolkit.Infrastructure.CorrelationId` | `CorrelationContext`, `UseCorrelationId()` middleware (headers from `CorrelationIdOptions`, `X-Correlation-Id`/`X-Attempt-Id` by default), `WithCorrelationId()` log enricher (property names overridable). |
| `Anarchy.Toolkit.Configuration` | `ModuleObservability`, which registers the `Observer` built by an `ObserverFactory` (vendor-neutral by default, overridable step by step). |
| `Anarchy.Toolkit.Testing` | `InMemoryObserver`, with in-memory log and metric sinks plus assertions, for tests. |

## Telemetry first: shared rules for logs and metrics

Think of logs and metrics as one story told in two formats. Keep the conventions aligned so searches, dashboards and alerts stay predictable. 🧭

### ✅ Golden rules

- **Use `Observer` wrappers first** (`Log*`, `ExecutionIncrementCount`, `ExecutionRecordDuration`) instead of direct `ILogger`/`MeasurerBase` calls.
- **Prefer `CallSiteEntity` overloads**, so class and method context is attached automatically.
- **Do not duplicate class/method context** in message text or metric names; the call site already carries it.
- **Pass `caller` into private helpers** that log or emit metrics, to keep the right context.
- **Use structured data** (template properties for logs, tags for metrics) instead of string composition.

### 🧩 Call-site pattern

```csharp
var caller = this.CalledFrom();

observer.LogInformation(caller, "Processing order {OrderId}", orderId);
observer.ExecutionIncrementCount(caller, 1, Tags.Success);
```

If `this.CalledFrom()` is not available (for example in a static method), build the call site explicitly:

```csharp
var caller = new CallSiteEntity("FooBarMapper", "Map");
observer.LogWarning(caller, "Falling back for {FooBarId}", fooBarId);
```

`this.ParentCaller()` returns the call site one frame up. The database policies use it to tag their duration metric with the repository method that triggered the query.

## Logging conventions

### ✍️ Message style

- Do not repeat class or method names in message templates.
- Use structured properties (`{OrderId}`, `{FooBarId}`, `{RetryCount}`) for filterable logs.
- Keep tense consistent:

| Situation | Convention | Example |
|---|---|---|
| Before action | Imperative | `"Processing FooBar order {OrderId}"` |
| After success | Past tense | `"Processed FooBar order {OrderId}"` |
| Failure | Start with `Failed ...` or `Error during ...` | `"Failed to process FooBar order {OrderId}"` |

### 🚨 Error/fatal logs

- Always pass the exception as the first argument to `LogError` / `LogFatal`.
- Include key identifiers in the message template.

```csharp
try
{
    await fooBarGateway.SendAsync(payload, cancellationToken);
}
catch (Exception ex)
{
    observer.LogError(ex, caller, "Failed to send FooBar payload for {OrderId}", orderId);
    throw;
}
```

### 🔗 Correlation/context properties

When a flow spans several log lines, push the shared context once:

```csharp
using (observer.LogContext.WithCorrelationId(correlationId))
using (observer.LogContext.PushProperties(new LogContextProperty("FooBarId", fooBarId)))
{
    observer.LogInformation(caller, "Started FooBar orchestration");
    // ... work
    observer.LogInformation(caller, "Completed FooBar orchestration");
}
```

`WithCorrelationId()` and `WithCallerSite()` come from `LogContextExtensions` in `Anarchy.Toolkit.Core.Observability`. The event pipeline adds its own `WithEvent(...)` helper, which pushes the aggregate and event identifiers for every processed event.

## Metric conventions

### 📈 Collection API

Prefer `observer.ExecutionIncrementCount(caller, value, tags)` over `observer.MeasurerBase.IncrementCounter(...)`.

### 🏷️ Metric suffixes

When recording metrics with custom suffixes, use the constants on `Observer` instead of hard-coded strings:

- `Observer.MetricSuffixExecTime` = `"execution_time"`: execution duration of operations
- `Observer.MetricSuffixExecCount` = `"execution_count"`: execution count of operations
- `Observer.MetricSuffixFailed` = `"failed"`: failed executions
- `Observer.MetricSuffixFailedAction` = `"failed.action"`: failed action attempts
- `Observer.MetricSuffixHealthPing` = `"health.ping"`: worker health checks
- `Observer.MetricSuffixHeartbeat` = `"heartbeat"`: heartbeats (used by `HeartbeatWorker`)
- `Observer.MetricSuffixNoHandlers` = `"no_handlers"`: no event handler matched (used by the event pipeline)
- `Observer.MetricSuffixDuplicatedUpdate` = `"duplicated_update"`: duplicate operations
- `Observer.MetricSuffixDuration` = `"duration"`: general duration tracking

```csharp
var caller = this.CalledFrom();

// Prefer execution wrappers for per-method metrics
observer.ExecutionRecordDuration(caller, elapsed, Tags.Success);
observer.ExecutionIncrementCount(caller, 1, Tags.Success);

// Use suffix constants for additional counters
observer.IncrementCounter(caller, Observer.MetricSuffixDuplicatedUpdate, 1, Tags.Success);
observer.RecordDuration(caller, Observer.MetricSuffixDuration, elapsed, Tags.Success);
```

For a whole async action, `observer.MonitoredActionAsync(caller, action)` records duration and count with the right outcome tag, and logs failures for you.

### 🏷️ Outcome tags

- Use `Tags.Success` and `Tags.Failure` for outcome signaling whenever applicable. Add more tags fluently: `Tags.Failure.WithTag("entity_state", "empty")`.
- Keep custom tag names domain-focused and in **snake_case** (`retry`, `result`, `subsidiary_id`).
- Keep boolean tag values lowercase (`"true"` / `"false"`).

### ⚠️ Keep cardinality low

- Metric tags must stay low-cardinality (roughly **<= 5 distinct values** per tag).
- If a value can vary widely (request paths, handler names, message counts, ids, timestamps, exception names), do **not** put it in metric tags.
- Put high-variance values in structured log properties instead, so they stay searchable without exploding the number of metric series.
- Prefer a simple outcome tag on the metric and attach detailed context to the logs in the same code path.

### 🚫 Avoid redundant metric naming

The call site already includes class and method context, so avoid metric names that repeat it:

```csharp
// bad: direct MeasurerBase access and redundant naming
observer.MeasurerBase.IncrementCounter("task_handler.fetch_fraud_score.handle", 1, Tags.Success);

// good: context comes from the caller via Observer wrappers
observer.ExecutionIncrementCount(caller, 1, Tags.Success);
```

## Helper method convention

When a private helper emits telemetry, accept `caller` as a parameter:

```csharp
public async Task HandleAsync(FooBarEvent @event, CancellationToken cancellationToken)
{
    var caller = this.CalledFrom();
    await ProcessItemsAsync(caller, @event.Items, cancellationToken);
}

private async Task ProcessItemsAsync(CallSiteEntity caller, IEnumerable<FooBarItem> items, CancellationToken cancellationToken)
{
    foreach (var item in items)
    {
        observer.LogDebug(caller, "Processing item {ItemId}", item.Id);
        observer.ExecutionIncrementCount(caller, 1, Tags.Success);
        await fooBarRepository.SaveAsync(item, cancellationToken);
    }
}
```

## Runtime collection

`ModuleObservability` (registered through `ModuleLibConfiguration`) asks an `ObserverFactory` for the `Observer` singleton. It reads an optional `Observability` configuration section:

```json
{
  "Observability": {
    "AppNamePrefix": "MyCompany."
  }
}
```

`AppNamePrefix` is prepended to the application name when missing; the result becomes the `app_name` log property and, snake-cased, the metric prefix.

### What the default factory does

Out of the box the toolkit is vendor-neutral:

- **Locally**: readable console logs at `Debug` level; metrics are printed by `ConsoleMeasurer`.
- **Hosted (Lambda, EC2 or containers)**: JSON console logs with the log context, correlation ids, structured exceptions and `app_name` (plus the AWS request context on Lambda). Metrics are written as structured log events by `LoggerMeasurer`, so nothing is lost before a metrics backend is plugged in.

### Plugging in an organisation's setup

Derive from `ObserverFactory` and override only the steps that differ, then hand it to `ModuleObservability`:

| Override | To change |
|---|---|
| `CorrelationIdPropertyName` / `AttemptIdPropertyName` | the log property names of the correlation ids |
| `Enrich` | the hosted log properties (team, cost center, instance ids, ...) |
| `WriteTo` | the hosted sinks (e.g. `Ec2CloudWatch(logGroup)`) |
| `CreateMeasurer` | the metrics backend (e.g. the Datadog measurers) |
| `CreateBaseLoggerConfiguration`, `CreateLocal`, `CreateHosted`, `Create` | everything else |

```csharp
public sealed class AcmeObserverFactory : ObserverFactory
{
    protected override LoggerConfiguration Enrich(LoggerConfiguration configuration, ObserverContext context) =>
        base.Enrich(configuration, context).Enrich.WithTeam("payments").Enrich.WithCostCenter("finance");

    protected override MeasurerBase CreateMeasurer(ObserverContext context, ILogger logger) =>
        new DatadogDistributionMeasurer();
}

services.UseModule(
    new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(
        new ModuleObservability(new AcmeObserverFactory(), config => new CorrelationIdOptions { CorrelationIdHeaderName = "Acme-CorrelationId" })),
    configuration,
    environment);
```

An organisation-wide factory, for example, could add Datadog metrics (the Lambda extension layer on Lambda, the DogStatsD agent on EC2), CloudWatch logging on EC2, the standard log properties under the organisation's names, and its own correlation headers, all without changing the toolkit.

## Testing telemetry

`TestCaseBase.FixtureObserver` is an `InMemoryObserver`, so logs and metrics can be asserted when observability *is* the behaviour under test:

```csharp
FixtureObserver.SinkLogs.Should().HaveMessage("Processed order {OrderId}");
FixtureObserver.InMemoryMeasurer.Should().HaveCounter("foo_bar_service.execute.execution_count");
```

Avoid asserting incidental logs or metrics in ordinary business-rule tests; assert the business result first. See [Testing Conventions](testing-conventions.md).
