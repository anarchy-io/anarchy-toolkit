# Observability

How logs and metrics are written through `Observer`: wording, naming, tags and cardinality. The [Observability guide](../guides/observability.md) explains the building blocks, where telemetry goes at runtime and how to plug in your organization's setup.

> **Recommendation, not a requirement.** The packages work without following this convention. It is a rule only for code in the toolkit repository (see [Contributing](../contributing/README.md)); applications can adopt it, adapt it or skip it. [Opting out or adapting](#opting-out-or-adapting) explains how.
{: .notice--info}

## Why

- **Logs and metrics describe the same events.** One call site gives both the same class and method context, so a dashboard and a log search line up. See [ADR 0004](../decisions/0004-observer-abstraction.md).
- **Searchable by default.** Structured properties and consistent tense make logs filterable without knowing who wrote them.
- **Cheap metrics.** Low-cardinality tags keep the number of metric series, and the bill, predictable.

## Telemetry first: shared rules for logs and metrics

Logs and metrics describe the same events in two formats. Keep their conventions aligned so that searches, dashboards and alerts remain predictable.

### General rules

- **Use `Observer` wrappers first** (`Log*`, `ExecutionIncrementCount`, `ExecutionRecordDuration`) instead of direct `ILogger`/`MeasurerBase` calls.
- **Prefer `CallSiteEntity` overloads**, so class and method context is attached automatically.
- **Do not duplicate class/method context** in message text or metric names; the call site already carries it.
- **Pass `caller` into private helpers** that log or emit metrics, to keep the right context.
- **Use structured data** (template properties for logs, tags for metrics) instead of string composition.

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

### Error/fatal logs

- Always pass the exception as the first argument to `LogError` / `LogFatal`.
- Include key identifiers in the message template.

```csharp
try
{
    await emailGateway.SendAsync(welcomeEmail, cancellationToken);
}
catch (Exception ex)
{
    observer.LogError(ex, caller, "Failed to send welcome email to {UserId}", userId);
    throw;
}
```

### Correlation/context properties

When a flow spans several log lines, push the shared context once:

```csharp
using (observer.LogContext.WithCorrelationId(correlationId))
using (observer.LogContext.PushProperties(new LogContextProperty("ClientId", clientId)))
{
    observer.LogInformation(caller, "Started client onboarding");
    // ...
    observer.LogInformation(caller, "Completed client onboarding");
}
```

`WithCorrelationId()` and `WithCallerSite()` come from `LogContextExtensions` in `Anarchy.Toolkit.Core.Observability`. The event pipeline adds its own `WithEvent(...)` helper, which pushes the aggregate and event identifiers for every processed event.

## Metric conventions

### Collection API

Prefer `observer.ExecutionIncrementCount(caller, value, tags)` over `observer.MeasurerBase.IncrementCounter(...)`.

### Metric suffixes

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

### Outcome tags

- Use `Tags.Success` and `Tags.Failure` for outcome signaling whenever applicable. Add more tags fluently: `Tags.Failure.WithTag("entity_state", "empty")`.
- Keep custom tag names domain-focused and in **snake_case** (`retry`, `result`, `client_tier`).
- Keep boolean tag values lowercase (`"true"` / `"false"`).

### Keep cardinality low

- Metric tags must stay low-cardinality (roughly **<= 5 distinct values** per tag).
- If a value can vary widely (request paths, handler names, message counts, ids, timestamps, exception names), do **not** put it in metric tags.
- Put high-variance values in structured log properties instead, so they stay searchable without exploding the number of metric series.
- Prefer a simple outcome tag on the metric and attach detailed context to the logs in the same code path.

### Avoid redundant metric naming

The call site already includes class and method context, so avoid metric names that repeat it:

```csharp
// Incorrect: direct MeasurerBase access and redundant naming
observer.MeasurerBase.IncrementCounter("user_handler.sync_user_profile.handle", 1, Tags.Success);

// Correct: context comes from the caller via Observer wrappers
observer.ExecutionIncrementCount(caller, 1, Tags.Success);
```

## Helper method convention

When a private helper emits telemetry, accept `caller` as a parameter:

```csharp
public async Task HandleAsync(UserRolesChangedEvent @event, CancellationToken cancellationToken)
{
    var caller = this.CalledFrom();
    await AssignRolesAsync(caller, @event.UserId, @event.Roles, cancellationToken);
}

private async Task AssignRolesAsync(CallSiteEntity caller, string userId, IEnumerable<UserRole> roles, CancellationToken cancellationToken)
{
    foreach (var role in roles)
    {
        observer.LogDebug(caller, "Assigning role {RoleId} to {UserId}", role.Id, userId);
        observer.ExecutionIncrementCount(caller, 1, Tags.Success);
        await userRoleRepository.SaveAsync(userId, role, cancellationToken);
    }
}
```

## Opting out or adapting

| You want to... | Do this |
|---|---|
| Keep [`ILogger<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1) ([Microsoft.Extensions.Logging](https://learn.microsoft.com/dotnet/core/extensions/logging)) or your current logging library | Keep it in your own code. Toolkit packages log through `Observer`; route both to the same sinks by building the `Observer` from your [Serilog](https://serilog.net/) logger in a custom [`ObserverFactory`](../guides/observability.md#plugging-in-an-organizations-setup). |
| Keep your metric names and tags | Override `CreateMeasurer` with a measurer that maps to your names, and keep emitting your existing metrics directly. |
| Run your legacy telemetry and `Observer` side by side while migrating | Register both; move call sites one at a time. See [Bringing your company stack](../guides/bringing-your-company-stack.md). |
| Use other property names (team, cost center, correlation id) | Override `Enrich`, `CorrelationIdPropertyName` and `AttemptIdPropertyName` in your factory. |

**What you lose by opting out:** the toolkit's own telemetry (workers, persistence policies, event pipeline) still goes through `Observer`; without a factory that shares your sinks, it ends up in the default console output instead of next to your logs.
