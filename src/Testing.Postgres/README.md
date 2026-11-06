# Anarchy.Toolkit.Testing.Postgres

PostgreSQL integration-test helpers for the toolkit's Npgsql persistence adapter.

```bash
dotnet add package Anarchy.Toolkit.Testing.Postgres
```

- Use `PostgresHelper` or `SingletonPostgresHelper` for connection, pool, resilience and unit-of-work fixtures.
- `PostgresOptionsMapper` maps options; helpers include generic enum type-handler registration and table cleanup.
- Tests need a PostgreSQL instance and a schema appropriate to the consuming application.

See [Infrastructure.Persistence.Postgres](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Postgres), the [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) and the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
