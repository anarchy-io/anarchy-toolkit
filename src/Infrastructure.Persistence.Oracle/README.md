# Anarchy.Toolkit.Infrastructure.Persistence.Oracle

Oracle adapter for the toolkit's relational persistence layer, using ODP.NET.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Oracle
```

- `OracleDbDialect`, connection pools, unit of work, resilience policy and repository bases adapt the shared relational layer to Oracle.
- Register `ModuleOracle` with connection and resilience options factories; `AddOracleHealthReports()` or `ModuleOracleHealthReport` adds reader and writer checks.
- `OracleConnectionOptions.DbName` is the service name; the default port is 1521. The connection pool applies command timeout and session time zone settings.
- Oracle SQL must use `:Name` binds and must not end in a semicolon. Use [Testing.Oracle](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Oracle) for test helpers.

## When to use it

Use this adapter when relational persistence runs on Oracle and your application needs the shared toolkit repository and unit-of-work model. The adapter supplies the ODP.NET integration, not the application schema, migrations or concrete event-store SQL.

## How to use it

1. Register an `Observer`, then use `ModuleOracle` with factories that build `OracleConnectionOptions` and `OracleResilienceOptions` from your host configuration. The [module guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/architecture.md#dependency-injection-modules) shows the equivalent factory pattern for PostgreSQL.
2. Configure reader and writer hosts, credentials, Oracle service name, time zone and command timeout; set `MaximumPoolSize` if the default of 100 is unsuitable. A null port selects 1521.
3. Implement concrete repositories using the Oracle bases and Oracle-compatible SQL. Use the `reader` and `writer` keyed policies as appropriate; the unkeyed policy uses the writer.
4. Add Oracle health reports when the host exposes service health, and exercise the real adapter with `Testing.Oracle` against a dedicated test schema.

See the [Oracle and persistence notes](https://github.com/anarchy-io/anarchy-toolkit#notes--follow-ups) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

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
