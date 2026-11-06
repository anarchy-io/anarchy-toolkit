# Anarchy.Toolkit.Infrastructure.Persistence.Postgres

PostgreSQL adapter for the toolkit's relational persistence layer, using Npgsql.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Postgres
```

- `PostgresDbDialect`, connection pools, unit of work, retry and resilience policies, and repository bases adapt the shared relational layer to PostgreSQL.
- Register `ModulePostgres` with factories for `PostgresConnectionOptions` and `PostgresResilienceOptions`.
- Register reader and writer health checks with `AddPostgresHealthReports()` or `ModulePostgresHealthReport`.
- Use [Testing.Postgres](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Postgres) for database test helpers.

See the [module registration example](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/architecture.md#dependency-injection-modules) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
