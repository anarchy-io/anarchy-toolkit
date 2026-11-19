# Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Oracle adapter for the toolkit's relational persistence layer, using ODP.NET.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle
```

- `OracleDbDialect`, connection pools, unit of work, resilience policy and repository bases adapt the shared relational layer to Oracle.
- Register `ModuleOracle` with connection and resilience options factories; `AddOracleHealthReports()` or `ModuleOracleHealthReport` adds reader and writer checks.
- `OracleConnectionOptions.DbName` is the service name; the default port is 1521. The connection pool applies command timeout and session time zone settings.
- Oracle SQL must use `:Name` binds and must not end in a semicolon. Use [Testing.Db.Oracle](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Db.Oracle) for test helpers.

## When to use it

Use this adapter when relational persistence runs on Oracle and your application needs the shared toolkit repository and unit-of-work model. The adapter supplies the ODP.NET integration, not the application schema, migrations or concrete event-store SQL.

## How to use it

1. Register `IAppTelemetry`, then use `ModuleOracle` with factories that build `OracleConnectionOptions` and `OracleResilienceOptions` from your host configuration. The [module guide](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html#dependency-injection-modules) shows the equivalent factory pattern for PostgreSQL.
2. Configure reader and writer hosts, credentials, Oracle service name, time zone and command timeout; set `MaximumPoolSize` if the default of 100 is unsuitable. A null port selects 1521.
3. Implement concrete repositories using the Oracle bases and Oracle-compatible SQL. Use the `reader` and `writer` keyed policies as appropriate; the unkeyed policy uses the writer.
4. Add Oracle health reports when the host exposes service health, and exercise the real adapter with `Testing.Db.Oracle` against a dedicated test schema.

See the [Oracle and persistence notes](https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html#provider-notes) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

### 1.0.0

- New package identity, renamed from `Anarchy.Toolkit.Infrastructure.Persistence.Oracle`; versions below are historical releases under that package ID.
- Preserve unit-of-work failures and use provider-neutral application telemetry.

### Historical releases under `Anarchy.Toolkit.Infrastructure.Persistence.Oracle`

### 1.2.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.2.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

### 1.1.6

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.1.5

- `System.Security.Cryptography.Pkcs` aligned with the .NET 10 LTS (10.0.15, down from 11.0.0).

### 1.1.4

- refactor(src): clean up code style across library packages

### 1.1.2

- build: migrate local automation to build tooling

### 1.1.1

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.1.0

- Applied the configurable maximum pool size to Oracle connection strings.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced the Oracle relational persistence provider, connection handling and provider-specific health reporting.
