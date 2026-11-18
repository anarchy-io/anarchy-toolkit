# Async Conventions

This guide defines how asynchronous code is written in the toolkit and in applications built on it, so that APIs and workers behave predictably under load and during shutdown.

> **Rules for the toolkit, recommendations for adopters.** The conventions in this guide are mandatory for code in the Anarchy I/O Toolkit repository and are enforced in review. Applications built on the platform are free to adopt, adapt or ignore them: they are offered as recommendations, not requirements.
{: .notice--info}

## Why it matters

In a short-lived host such as AWS Lambda, each invocation is bounded by the platform, so missing cancellation support is rarely visible.

Long-lived workers (`Anarchy.Toolkit.Hosting.Workers` on EC2 or ECS) are different. Without cancellation:

- long-running calls block processing loops,
- shutdown is delayed until in-flight calls complete or time out,
- retries and recovery become harder to reason about.

Apply these rules consistently, even in code that currently runs only in short-lived hosts.

## Naming rule

Use the `Async` suffix for asynchronous methods.

Examples:

- `GetUserByIdAsync`
- `RunInManagedSessionAsync` (toolkit)
- `ExecuteAsync` (toolkit)

## Cancellation rule

Expose an explicitly optional token on async methods:

```csharp
CancellationToken? cancellationToken = null
```

Typical signature pattern (as in `IStoragePolicy<TStorage>.ExecuteAsync`):

```csharp
Task<TResult> ExecuteAsync(..., CancellationToken? cancellationToken = null)
```

Call sites that have no token stay concise, while worker, host or request cancellation still flows through when a token is available.

## Propagation rule

If a method accepts `CancellationToken? cancellationToken = null`, pass it downstream wherever supported.

For database queries specifically:

- use command-level cancellation (for example Dapper's `CommandDefinition`) for calls that can hang or run long,
- keep simple, low-risk queries straightforward when cancellation is already bounded by other controls (timeouts, retries, short-lived operations).

`CommandDefinition` is not mandatory for every query; use it where a query can block for an unbounded time.

Example in a repository deriving from `PostgresDbReaderRepositoryBase`:

```csharp
public async Task<IReadOnlyCollection<UserEntity>> GetPendingActivationAsync(CancellationToken? cancellationToken = null)
{
    return await dbPolicy.ExecuteAsync(async (connEnvelope, callbackToken) => {
        var token = cancellationToken ?? callbackToken ?? CancellationToken.None;

        // Use CommandDefinition for potentially hanging or heavy queries.
        var command = new CommandDefinition(
            commandText: LoadSql("User.Get.PendingActivation"),
            parameters: new { Limit = 100 },
            cancellationToken: token
        );

        var rows = await connEnvelope.DataStorageConnection.QueryAsync<UserEntity>(command);
        return rows.ToArray();
    }, operationName: "User.Get.PendingActivation", cancellationToken: cancellationToken);
}
```

The `operationName` is also attached to the database-policy duration metric as the `operation_name` tag. When omitted, the policy falls back to the calling method's call site.

## Worker-focused guidance

In workers built on `WorkerBase` / `MonitoredWorkerBase` / `ResilientWorkerBase`:

- pass the worker's `stoppingToken` from `RunIterationAsync` into use cases and repositories,
- make polling and publishing loops stoppable,
- avoid uncancelable network or database calls in shutdown paths.

Following these rules results in prompt shutdowns, fewer stuck operations and more predictable recovery for continuously running workers.
