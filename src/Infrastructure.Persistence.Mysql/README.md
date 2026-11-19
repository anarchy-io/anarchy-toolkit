# Anarchy.Toolkit.Infrastructure.Persistence.Mysql

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

MySQL adapter for the toolkit's relational persistence layer, using MySqlConnector.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Mysql
```

- `MysqlDbDialect`, connection pools, unit of work, retry policy and repository bases adapt the shared relational layer to MySQL.
- Register `IAppTelemetry` in DI and configure `ModuleMysql` with connection and resilience options factories; `AddMysqlHealthReports()` adds reader and writer checks.
- The unkeyed storage policy uses the writer; use the `reader` key for read-only operations. Port 3306 is the default.
- Named session time zones require populated MySQL time-zone tables. Retry the whole unit of work rather than individual commands inside a transaction.

## When to use it

Use this adapter for a MySQL-backed application that needs the toolkit's relational policies, unit of work and repository bases. The consuming application supplies its schema, migrations and MySQL-specific queries.

## How to use it

1. Register `IAppTelemetry` and use `ModuleMysql` with factories for `MysqlConnectionOptions` and `MysqlResilienceOptions`. Obtain hosts and credentials from the environment or a secret provider rather than committing them.
2. Configure reader/writer hosts, database name, command timeout and optional session time zone. The port defaults to 3306 and `MaximumPoolSize` defaults to 100; tune pool size for the database and number of application instances.
3. Implement repositories with the MySQL bases and select the keyed `reader` policy for read-only work. The unkeyed policy uses the writer. Add `AddMysqlHealthReports()` if the host should check both endpoints.
4. Validate the integration against a dedicated MySQL test database using `Testing.Mysql`. If a transaction fails, retry the whole unit of work rather than individual statements inside it.

See the [MySQL usage notes](https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html#mysql) and [Testing.Mysql](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Mysql).

## Releases

### 1.2.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.2.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

### 1.1.5

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.1.4

- refactor(src): clean up code style across library packages
- chore(deps): update AWS SDK, Lambda ASP.NET Core server and MySqlConnector packages

### 1.1.2

- build: migrate local automation to build tooling

### 1.1.1

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.1.0

- Applied the configurable maximum pool size to MySQL connection strings.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced the MySQL relational persistence provider, connection handling and provider-specific health reports.
