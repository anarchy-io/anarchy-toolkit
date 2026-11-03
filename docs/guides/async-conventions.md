# Async Conventions

This guide explains how async code is written in the toolkit, so APIs and workers built on it behave predictably. ✨

## Why this matters (more than it looks)

When an application is deployed as a Lambda, cancellation can feel less urgent day-to-day: AWS spins up a fresh invocation and keeps moving.

But toolkit packages are also used by long-lived workers (`Anarchy.Toolkit.Hosting.Workers` on EC2 or ECS), and there things change fast:

- long-running calls can block loops,
- shutdown can become slow,
- retries and recovery can get messy.

So this can feel like overkill at first, but in worker mode it is absolutely worth it. 🧠

## 🏷️ Naming rule

Use the `Async` suffix for asynchronous methods.

Examples from the toolkit:

- `GetLatestIdAsync`
- `RunInManagedSessionAsync`
- `ExecuteAsync`

## 🧵 Cancellation rule

Expose an explicitly optional token on async methods:

```csharp
CancellationToken? cancellationToken = null
```

Typical signature pattern (as in `IStoragePolicy<TStorage>.ExecuteAsync`):

```csharp
Task<TResult> ExecuteAsync(..., CancellationToken? cancellationToken = null)
```

This keeps call sites clean while still letting worker, host or request cancellation flow through when needed.

## 🔄 Propagation rule

If a method accepts `CancellationToken? cancellationToken = null`, pass it downstream wherever supported.

For database queries specifically:

- use command-level cancellation (for example Dapper's `CommandDefinition`) for calls that can hang or run long,
- keep simple, low-risk queries straightforward when cancellation is already bounded by other controls (timeouts, retries, short-lived operations).

So this is not a blanket rule to force `CommandDefinition` everywhere; it is a practical tool to apply where the risk is higher. 🎯

Example in a repository deriving from `PostgresDbReaderRepositoryBase`:

```csharp
public async Task<IReadOnlyCollection<MyEntity>> GetPendingAsync(CancellationToken? cancellationToken = null)
{
    return await dbPolicy.ExecuteAsync(async (connEnvelope, callbackToken) => {
        var token = cancellationToken ?? callbackToken ?? CancellationToken.None;

        // Use CommandDefinition for potentially hanging or heavy queries.
        var command = new CommandDefinition(
            commandText: LoadSql("MyEntity.Get.Pending"),
            parameters: new { Limit = 100 },
            cancellationToken: token
        );

        var rows = await connEnvelope.DataStorageConnection.QueryAsync<MyEntity>(command);
        return rows.ToArray();
    }, operationName: "MyEntity.Get.Pending", cancellationToken: cancellationToken);
}
```

The `operationName` is also attached to the database-policy duration metric as the `operation_name` tag. When omitted, the policy falls back to the calling method's call site.

## ⚙️ Worker-focused guidance

In workers built on `WorkerBase` / `MonitoredWorkerBase` / `ResilientWorkerBase`:

- pass the worker's `stoppingToken` from `RunIterationAsync` into use cases and repositories,
- make polling and publishing loops stoppable,
- avoid uncancellable network or database calls in shutdown paths.

This gives cleaner shutdowns, fewer stuck operations, and better resilience for continuously running workers. 💙
