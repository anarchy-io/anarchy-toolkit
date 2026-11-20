# Observability

How logs and metrics are written through `AppTelemetry`: wording, naming, tags and cardinality. The [Observability guide](../guides/observability.md) explains the building blocks, where telemetry goes at runtime, correlation IDs and how to plug in your organization's backend.

> **Recommendation, not a requirement.** The packages work without following this convention. It is a rule only for code in the toolkit repository (see [Contributing](../contributing/README.md)); applications can adopt it, adapt it or skip it. [Opting out or adapting](#opting-out-or-adapting) explains how.
{: .notice--info}

## Why

- **Logs and metrics describe the same events.** One call site gives both the same class and method context, so a dashboard and a log search line up. See [ADR 0004](../decisions/0004-telemetry-composition.md).
- **Searchable by default.** Structured properties and consistent tense make logs filterable without knowing who wrote them.
- **Cheap metrics.** Low-cardinality tags keep the number of metric series, and the bill, predictable.
- **Any backend.** The conventions sit on the standard Microsoft logging and .NET metrics APIs, so they hold whichever provider or exporter the application chooses.

## Telemetry first: shared rules for logs and metrics

Logs and metrics describe the same events in two formats. Keep their conventions aligned so that searches, dashboards and alerts remain predictable.

### General rules

- **Inject `AppTelemetry`** and use its wrappers (`Log*`, `IncrementCounter`, `RecordDuration`, `StartTimer`, `MonitoredActionAsync`) instead of calling `ILogger` or `IMetricRecorder` directly. Inject `ICoreTelemetry` only when the code needs nothing beyond the logger and the recorder.
- **Prefer the `CallSite` overloads**, so class and method context is attached automatically.
- **Do not duplicate class/method context** in message text or metric names; the call site already carries it.
- **Pass `caller` into private helpers** that log or emit metrics, to keep the right context.
- **Use structured data** (template properties for logs, tags for metrics) instead of string composition.
- **Never create a private logger, meter or static pipeline.** Resolve telemetry from DI so everything reaches the application's providers.

## Logging conventions

### Message style

- Do not repeat class or method names in message templates.
- Use structured properties (`{UserId}`, `{ClientId}`, `{RetryCount}`) for filterable logs.
- Keep tense consistent:

| Situation | Convention | Example |
|---|---|---|
| Before action | Imperative | `"Creating user {UserId}"` |
| After success | Past tense | `"Created user {UserId}"` |
| Failure | Start with `Failed ...` or `Error during ...` | `"Failed to create user {UserId}"` |

### Error/critical logs

- Always pass the exception to `LogError` / `LogCritical`.
- Include key identifiers in the message template.

```csharp
try
{
    await emailGateway.SendAsync(welcomeEmail, cancellationToken);
}
catch (Exception ex)
{
    telemetry.LogError(caller, ex, "Failed to send welcome email to {UserId}", userId);
    throw;
}
```

### Notable events

For an occurrence that people alert on or search for by name, log with a **stable `EventId`** and structured properties, and count it when the number matters:

```csharp
telemetry.Log(LogLevel.Warning, caller, new EventId(4001, "order.payment_declined"), null,
    "Payment declined for order {OrderId}", orderId);
telemetry.IncrementCounter(caller, "payment_declined", 1, new("reason", "declined"));
```

Keep the `EventId` number and name unchanged once published: filters, alerts and vendor routing rules depend on them. There is no title/text event API; routing to Datadog Events or New Relic custom events is a logging provider or exporter concern (see the [guide](../guides/observability.md#notable-events)).

### Scopes and context properties

When a flow spans several log lines, push the shared context once. `LogContext` opens a standard `ILogger` scope, so every provider that supports scopes attaches the properties:

```csharp
using (telemetry.LogContext.PushProperties(new("ClientId", clientId), new("Plan", plan)))
{
    telemetry.LogInformation(caller, "Started client onboarding");
    // ...
    telemetry.LogInformation(caller, "Completed client onboarding");
}
```

Correlation and attempt IDs are not pushed by hand: the middleware, the workers and `CorrelationContext.BeginScope(...)` (or `message.BeginCorrelationScope(logger)` for SQS messages) open their scope for the duration of the work. See [Correlation IDs](../guides/observability.md#correlation-ids). The event pipeline adds its own `WithEvent(...)` helper, which pushes the aggregate and event identifiers for every processed event.

## Metric conventions

### Collection API

Prefer `telemetry.IncrementCounter(caller, suffix, amount, tags)` and `telemetry.RecordDuration(caller, duration, tags)` over `telemetry.Metrics.*`: the call-site overloads build the standard name `{class_name}.{method_name}.{suffix}`. Durations are recorded in seconds.

### Metric suffixes

When recording metrics with custom suffixes, use the constants on `MetricSuffixes` instead of hard-coded strings:

- `MetricSuffixes.ExecutionTime` = `"execution_time"`: execution duration of operations
- `MetricSuffixes.ExecutionCount` = `"execution_count"`: execution count of operations
- `MetricSuffixes.Failed` = `"failed"`: failed executions
- `MetricSuffixes.FailedAction` = `"failed.action"`: failed action attempts
- `MetricSuffixes.HealthPing` = `"health.ping"`: worker health checks
- `MetricSuffixes.Heartbeat` = `"heartbeat"`: heartbeats (used by `HeartbeatWorker`)
- `MetricSuffixes.NoHandlers` = `"no_handlers"`: no event handler matched (used by the event pipeline)

Other suffixes (for example `duplicated_update`) are plain `snake_case` strings next to the code that records them.

```csharp
var caller = this.CalledFrom();

// Prefer the execution wrappers for per-method metrics
telemetry.RecordDuration(caller, elapsed, new("success", true));
telemetry.IncrementCounter(caller, MetricSuffixes.ExecutionCount, 1, new("success", true));

// Use a suffix for additional counters
telemetry.IncrementCounter(caller, "duplicated_update", 1, new("success", true));
```

For a whole async action, `telemetry.MonitoredActionAsync(caller, action, cancellationToken)` records duration and count tagged `success` (and `cancelled` when the token is cancelled), logs failures for you and rethrows. For a block of code, `using var timer = telemetry.StartTimer(caller);` records its duration when disposed.

### Outcome tags

- Tag outcomes with `success` (`true` / `false`) whenever applicable, as `MonitoredActionAsync` does.
- Keep custom tag names domain-focused and in **snake_case** (`retry`, `result`, `client_tier`).
- Keep boolean tag values lowercase when they are strings (`"true"` / `"false"`), or pass real booleans.

### Keep cardinality low

- Metric tags must stay low-cardinality (roughly **<= 5 distinct values** per tag).
- If a value can vary widely (request paths, handler names, message counts, ids, timestamps, exception names), do **not** put it in metric tags.
- Put high-variance values in structured log properties instead, so they stay searchable without exploding the number of metric series.
- Prefer a simple outcome tag on the metric and attach detailed context to the logs in the same code path.

### Avoid redundant metric naming

The call site already includes class and method context, so avoid metric names that repeat it:

```csharp
// Incorrect: direct recorder access and redundant naming
telemetry.Metrics.IncrementCounter("user_handler.sync_user_profile.handle", 1, new("success", true));

// Correct: context comes from the caller via AppTelemetry
telemetry.IncrementCounter(caller, MetricSuffixes.ExecutionCount, 1, new("success", true));
```

## Helper method convention

When a private helper emits telemetry, accept `caller` as a parameter:

```csharp
public async Task HandleAsync(UserRolesChangedEvent @event, CancellationToken cancellationToken)
{
    var caller = this.CalledFrom();
    await AssignRolesAsync(caller, @event.UserId, @event.Roles, cancellationToken);
}

private async Task AssignRolesAsync(CallSite caller, string userId, IEnumerable<UserRole> roles, CancellationToken cancellationToken)
{
    foreach (var role in roles)
    {
        telemetry.LogDebug(caller, "Assigning role {RoleId} to {UserId}", role.Id, userId);
        telemetry.IncrementCounter(caller, MetricSuffixes.ExecutionCount, 1, new("success", true));
        await userRoleRepository.SaveAsync(userId, role, cancellationToken);
    }
}
```

## Opting out or adapting

| You want to... | Do this |
|---|---|
| Keep [`ILogger<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1) in your own code | Keep it. Toolkit packages log through `AppTelemetry`, which writes to the same `ILoggerFactory`, so both reach the same providers. |
| Keep [Serilog](https://serilog.net/) or another logging library | Register it as a logging provider (`AddSerilogTelemetry(...)` for Serilog). The instrumentation code does not change. |
| Keep your metric names and tags | Emit your existing metrics directly with `System.Diagnostics.Metrics`, and register an `IMetricRecorder` that maps the toolkit's names to yours (or decorates the default). |
| Use other property names for correlation | Set them in `CorrelationIdOptions`; see the [guide](../guides/observability.md#correlation-ids). |
| Send telemetry to Datadog, New Relic or another vendor | Configure an OTLP exporter or the vendor's logging provider; see the [guide](../guides/observability.md#sending-telemetry-to-a-backend). |

**What you lose by opting out:** the toolkit's own telemetry (workers, persistence policies, event pipeline) still goes through `AppTelemetry`; without a shared logging and metrics pipeline it ends up in the default providers instead of next to your own telemetry.
