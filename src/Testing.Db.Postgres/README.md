# Anarchy.Toolkit.Testing.Db.Postgres

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Testing kit · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

PostgreSQL integration-test helpers for the toolkit's Npgsql persistence adapter.

```bash
dotnet add package Anarchy.Toolkit.Testing.Db.Postgres
```

- Use `PostgresHelper` or `SingletonPostgresHelper` for connection, pool, resilience and unit-of-work fixtures.
- `PostgresOptionsMapper` maps options; helpers include generic enum type-handler registration and table cleanup.
- Tests need a PostgreSQL instance and a schema appropriate to the consuming application.

## When to use it

Use this package for integration tests that need to verify PostgreSQL queries, Dapper mappings or unit-of-work behavior against a real database. It creates fixtures using the toolkit provider; it does not launch PostgreSQL or migrate your schema.

## How to use it

1. Start an isolated PostgreSQL instance and prepare the test schema. Implement `IPostgresAppSettings` on your settings type; policy fixtures additionally require `IResilienceAppSettings`.
2. Create the required fixture from embedded test settings, or pass a settings instance directly:

```csharp
using Anarchy.Toolkit.Testing.Db.Postgres.Helpers.Database;

var policy = PostgresHelper.DbPolicyFixture<TestAppSettings>();
var unitOfWork = PostgresHelper.DbUnitOfWorkFixture<TestAppSettings>();
```

Here `TestAppSettings` is your own type and its declaring assembly contains the embedded test settings. Register enum handlers as needed, use the fixtures in repository tests, and clean up only dedicated test tables. `SingletonPostgresHelper` shares a policy; coordinate access and cleanup when tests run in parallel.

See [Infrastructure.Persistence.Db.Postgres](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres), the [testing conventions](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html) and the [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
