# Anarchy.Toolkit.Infrastructure.Persistence.Oracle

Oracle adapter for the toolkit's relational persistence layer, using ODP.NET.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Oracle
```

- `OracleDbDialect`, connection pools, unit of work, resilience policy and repository bases adapt the shared relational layer to Oracle.
- Register `ModuleOracle` with connection and resilience options factories; `AddOracleHealthReports()` or `ModuleOracleHealthReport` adds reader and writer checks.
- `OracleConnectionOptions.DbName` is the service name; the default port is 1521. The connection pool applies command timeout and session time zone settings.
- Oracle SQL must use `:Name` binds and must not end in a semicolon. Use [Testing.Oracle](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Oracle) for test helpers.

See the [Oracle and persistence notes](https://github.com/anarchy-io/anarchy-toolkit#notes--follow-ups) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
