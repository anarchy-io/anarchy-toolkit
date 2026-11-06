# Anarchy.Toolkit.Testing.Oracle

Oracle integration-test helpers for the toolkit's ODP.NET persistence adapter.

```bash
dotnet add package Anarchy.Toolkit.Testing.Oracle
```

- Use `OracleHelper` or `SingletonOracleHelper` for connection, pool, resilience and unit-of-work fixtures.
- `OracleOptionsMapper` maps options; helpers include generic enum type-handler registration and table cleanup.
- Tests need an Oracle instance and a schema appropriate to the consuming application. Truncation uses plain `TRUNCATE TABLE`, without identity restart or cascade.

See [Infrastructure.Persistence.Oracle](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Oracle), the [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) and the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
