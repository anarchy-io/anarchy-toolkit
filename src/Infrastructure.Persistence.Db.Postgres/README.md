# Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

PostgreSQL adapter for the toolkit's relational persistence layer, using Npgsql.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres
```

- `PostgresDbDialect`, connection pools, unit of work, retry and resilience policies, and repository bases adapt the shared relational layer to PostgreSQL.
- Register `ModulePostgres` with factories for `PostgresConnectionOptions` and `PostgresResilienceOptions`.
- Register reader and writer health checks with `AddPostgresHealthReports()` or `ModulePostgresHealthReport`.
- Use [Testing.Db.Postgres](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Db.Postgres) for database test helpers.

## When to use it

Use this provider when a toolkit application stores relational data in PostgreSQL and needs Npgsql-backed pools, repositories, unit-of-work coordination and optional health checks. Your application still owns its schema, migrations and SQL.

## How to use it

Register `AppTelemetry` through `ModuleTelemetry` (or supply a consumer implementation) before configuring the module with factories that read the application's configuration. For example, with configuration, environment and section binding already set up:

```csharp
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Entity.Configuration.Storage.Database;
using Microsoft.Extensions.Configuration;

services.UseModule(
    new ModulePostgres(
        config => config.GetSection("Postgres").Get<PostgresConnectionOptions>()!,
        config => new PostgresResilienceOptions(config.GetValue<int>("Resilience:RetryCount"))),
    configuration, environment);
```

Populate and validate reader/writer hosts, credentials, database name, time zone and command timeout in that section; a null port uses 5432. Set `MaximumPoolSize` when the default of 100 is unsuitable. The unkeyed storage policy uses the writer; keyed `reader` and `writer` policies select the corresponding pools. Register health reports separately and use provider repository bases with your own PostgreSQL SQL.

See the [module registration example](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html#dependency-injection-modules) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
