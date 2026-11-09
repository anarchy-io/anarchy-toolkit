# Anarchy.Toolkit.Infrastructure.Persistence.Db

Provider-agnostic relational persistence infrastructure for toolkit database adapters.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Db
```

- `IDbDialect`, connection-pool and options bases, unit of work, resilience policy and retry builder provide the common database layer.
- Build repositories on the reader, writer and batch-writer bases; database health reports use the dialect's health-check SQL.
- Transactional outbox and event-store contracts, records and repository bases are included. Consumers supply provider-specific concrete repositories and SQL.
- Use a provider adapter for PostgreSQL, Oracle or MySQL, or implement a new dialect and adapter.

## When to use it

Use this package when implementing a relational adapter or shared repository behavior. Most applications should reference a provider-specific adapter instead: this package supplies common policies and base classes but does not choose a driver, provision a schema or provide concrete SQL for an application's tables.

## How to use it

1. Select an existing provider, or implement `IDbDialect`, connection-string and pool types, a unit of work, retry policy and provider-specific repository bases for a new database.
2. Derive concrete reader/writer repositories and provide their SQL and mappings. For event-store or outbox functionality, supply the provider-specific concrete repositories and embedded SQL as described in the provider guide.
3. Configure reader and writer hosts, credentials, command timeout and resilience settings at the host. `DbConnectionOptions.MaximumPoolSize` defaults to 100 and is applied by the included providers.
4. Register the provider module and optional health reports, then inject the repository and unit-of-work abstractions into application services. Define transaction and retry boundaries explicitly, especially when an operation has non-database side effects.

See [adding a database provider](https://github.com/anarchy-io/anarchy-toolkit#adding-a-database-provider) and the [persistence notes](https://github.com/anarchy-io/anarchy-toolkit#notes--follow-ups), including known limitations.

## Releases

### 1.1.0

- Added configurable maximum database connection pool size.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced provider-agnostic relational repositories, units of work, connection configuration, event store support and health reporting.
