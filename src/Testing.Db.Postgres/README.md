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
using Anarchy.Toolkit.Testing.Db.Postgres.Helper;

var policy = PostgresHelper.DbPolicyFixture<TestAppSettings>();
var unitOfWork = PostgresHelper.DbUnitOfWorkFixture<TestAppSettings>();
```

Here `TestAppSettings` is your own type and its declaring assembly contains the embedded test settings. Register enum handlers as needed, use the fixtures in repository tests, and clean up only dedicated test tables. `SingletonPostgresHelper` shares a policy; coordinate access and cleanup when tests run in parallel.

See [Infrastructure.Persistence.Db.Postgres](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres), the [testing conventions](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html) and the [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

### 1.0.0

- New package identity, renamed from `Anarchy.Toolkit.Testing.Postgres`; versions below are historical releases under that package ID.
- Include the unit-of-work failure handling and provider-neutral telemetry changes in the shared database test kit.

### Historical releases under `Anarchy.Toolkit.Testing.Postgres`

### 1.2.2

- build(deps): drop package references already resolved transitively

### 1.2.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.2.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

### 1.1.6

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.1.5

- refactor(src): clean up code style across library packages
- chore(deps): update AWS SDK, Lambda ASP.NET Core server and MySqlConnector packages

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
