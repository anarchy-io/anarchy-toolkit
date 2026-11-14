# Anarchy.Toolkit.Infrastructure.Persistence.Postgres

PostgreSQL adapter for the toolkit's relational persistence layer, using Npgsql.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Postgres
```

- `PostgresDbDialect`, connection pools, unit of work, retry and resilience policies, and repository bases adapt the shared relational layer to PostgreSQL.
- Register `ModulePostgres` with factories for `PostgresConnectionOptions` and `PostgresResilienceOptions`.
- Register reader and writer health checks with `AddPostgresHealthReports()` or `ModulePostgresHealthReport`.
- Use [Testing.Postgres](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Postgres) for database test helpers.

## When to use it

Use this provider when a toolkit application stores relational data in PostgreSQL and needs Npgsql-backed pools, repositories, unit-of-work coordination and optional health checks. Your application still owns its schema, migrations and SQL.

## How to use it

Register an `Observer` first, then configure the module with factories that read the application's configuration. For example, with configuration, environment and section binding already set up:

```csharp
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;
using Microsoft.Extensions.Configuration;

services.UseModule(
    new ModulePostgres(
        config => config.GetSection("Postgres").Get<PostgresConnectionOptions>()!,
        config => new PostgresResilienceOptions(config.GetValue<int>("Resilience:RetryCount"))),
    configuration, environment);
```

Populate and validate reader/writer hosts, credentials, database name, time zone and command timeout in that section; a null port uses 5432. Set `MaximumPoolSize` when the default of 100 is unsuitable. The unkeyed storage policy uses the writer; keyed `reader` and `writer` policies select the corresponding pools. Register health reports separately and use provider repository bases with your own PostgreSQL SQL.

See the [module registration example](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/architecture.md#dependency-injection-modules) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.1.4

- refactor(src): clean up code style across library packages

### 1.1.2

- build: migrate local automation to build tooling

### 1.1.1

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.1.0

- Applied the configurable maximum pool size to PostgreSQL connection strings.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced the PostgreSQL relational persistence provider, connection handling and provider-specific health reporting.
