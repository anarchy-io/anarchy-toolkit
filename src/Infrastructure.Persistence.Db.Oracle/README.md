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

1. Register `AppTelemetry`, then use `ModuleOracle` with factories that build `OracleConnectionOptions` and `OracleResilienceOptions` from your host configuration. The [module guide](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html#dependency-injection-modules) shows the equivalent factory pattern for PostgreSQL.
2. Configure reader and writer hosts, credentials, Oracle service name, time zone and command timeout; set `MaximumPoolSize` if the default of 100 is unsuitable. A null port selects 1521.
3. Implement concrete repositories using the Oracle bases and Oracle-compatible SQL. Use the `reader` and `writer` keyed policies as appropriate; the unkeyed policy uses the writer.
4. Add Oracle health reports when the host exposes service health, and exercise the real adapter with `Testing.Db.Oracle` against a dedicated test schema.

See the [Oracle and persistence notes](https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html#provider-notes) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [Dapper](https://www.nuget.org/packages/Dapper) (also through `Infrastructure.Persistence.Db`) | Runs the repositories' SQL and maps results | Yes: through `Infrastructure.Persistence.Db`'s type handlers |
| [Polly](https://www.nuget.org/packages/Polly.Core) (`Polly.Core`, through `Infrastructure.Persistence.Db`) | Retries transient database failures | Yes: through the resilience policy builder |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
