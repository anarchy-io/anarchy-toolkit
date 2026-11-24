# Anarchy.Toolkit.Infrastructure.Persistence.Db

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Provider-agnostic relational persistence infrastructure for toolkit database adapters.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Db
```

- `IDbDialect`, connection-pool and options bases, unit of work, resilience policy and retry builder provide the common database layer.
- Build repositories on the reader, writer and batch-writer bases; database health reports use the dialect's health-check SQL.
- For event-store bases and transactional Outbox contracts, use `Anarchy.Toolkit.Infrastructure.Persistence.Db.Events`; applications implement their own Outbox repositories.
- Use a provider adapter for PostgreSQL, Oracle or MySQL, or implement a new dialect and adapter.

## When to use it

Use this package when implementing a relational adapter or shared repository behavior. Most applications should reference a provider-specific adapter instead: this package supplies common policies and base classes but does not choose a driver, provision a schema or provide concrete SQL for an application's tables.

## How to use it

1. Select an existing provider, or implement `IDbDialect`, connection-string and pool types, a unit of work, retry policy and provider-specific repository bases for a new database.
2. Derive concrete reader/writer repositories and provide their SQL and mappings.
3. Configure reader and writer hosts, credentials, command timeout and resilience settings at the host. `DbConnectionOptions.MaximumPoolSize` defaults to 100 and is applied by the included providers.
4. Register the provider module and optional health reports, then inject the repository and unit-of-work abstractions into application services. Define transaction and retry boundaries explicitly, especially when an operation has non-database side effects.

A unit-of-work instance rejects nested and concurrent sessions. The code that starts a session owns its transaction:
`RunInJoinedSessionAsync` lets a participant, such as an aggregate called by a UseCase, share the caller's transaction
without being able to begin, commit or roll it back (`TransactionNotOwnedException`), and
`RunInJoinedOrManagedSessionAsync` joins the caller's transaction when there is one and otherwise owns a managed session.
`BeginAsync`, `CommitAsync` and `RollbackAsync` only act on the session the instance started in the current async flow.

A transaction is rolled back only when it exists and is still pending, including one a `RunInSessionAsync` callback left
open, and a rolled-back or abandoned transaction publishes `RolledBackUowTransactionLocalEvent`. Cancellation is
observed while connecting, beginning, running the callback and just before an automatic commit, and propagates after
rollback and cleanup, which use an uncancelled token; a commit is never interrupted once started. If rollback also
fails, inspect the original exception's `Data` for `UnitOfWork.RollbackFailure`; if cleanup fails while another exception
is already being propagated, inspect that exception's `Data` for `UnitOfWork.CleanupFailure`. Connections are discarded
after a failed begin, commit, rollback or transaction disposal instead of being returned to the pool. Each session can
start only one transaction; further calls to `BeginAsync` fail with `TransactionAlreadyActiveException`. If transaction
and connection cleanup both fail, the connection failure is available on the transaction failure under
`UnitOfWork.ConnectionCleanupFailure`.

See [adding a database provider](https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html#adding-a-database-provider) and the [persistence notes](https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html#provider-notes), including known limitations.

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
