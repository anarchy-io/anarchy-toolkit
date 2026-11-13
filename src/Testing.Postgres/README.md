# Anarchy.Toolkit.Testing.Postgres

PostgreSQL integration-test helpers for the toolkit's Npgsql persistence adapter.

```bash
dotnet add package Anarchy.Toolkit.Testing.Postgres
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
using Anarchy.Toolkit.Testing.Postgres.Helper;

var policy = PostgresHelper.DbPolicyFixture<TestAppSettings>();
var unitOfWork = PostgresHelper.DbUnitOfWorkFixture<TestAppSettings>();
```

Here `TestAppSettings` is your own type and its declaring assembly contains the embedded test settings. Register enum handlers as needed, use the fixtures in repository tests, and clean up only dedicated test tables. `SingletonPostgresHelper` shares a policy; coordinate access and cleanup when tests run in parallel.

See [Infrastructure.Persistence.Postgres](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Postgres), the [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) and the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.1.4

- chore(deps): update AWS SDK packages

### 1.1.2

- build: migrate local automation to build tooling

### 1.1.1

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.1.0

- Updated the database testing and PostgreSQL persistence dependencies alongside configurable maximum pool size support.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced PostgreSQL integration test helpers, including a singleton variant and options mapper.
