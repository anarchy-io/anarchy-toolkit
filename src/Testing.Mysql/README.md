# Anarchy.Toolkit.Testing.Mysql

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Testing kit · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

MySQL integration-test helpers for the toolkit's MySqlConnector persistence adapter.

```bash
dotnet add package Anarchy.Toolkit.Testing.Mysql
```

- Use `MysqlHelper` or `SingletonMysqlHelper` for database fixtures and `MysqlOptionsMapper` for options mapping.
- `IMysqlAppSettings` supports settings-based fixtures; helpers also provide enum handlers and table cleanup.
- Tests need a MySQL instance and an application schema. MySQL `TRUNCATE` resets auto-increment and may fail for referenced tables.

## When to use it

Use this package in integration tests that must verify actual MySQL SQL, mappings or transaction behavior. It creates toolkit persistence fixtures against a configured server; it does not start MySQL or create application tables.

## How to use it

1. Provision an isolated MySQL database and schema. Implement `IMysqlAppSettings` on your test settings type; also implement `IResilienceAppSettings` when creating a resilience policy fixture.
2. Use `MysqlHelper.DbPolicyFixture<TAppSettings>()` or pass a settings instance directly; use `DbUnitOfWorkFixture` for unit-of-work tests. The generic overload reads embedded test settings from the settings type's assembly.
3. Register enum handlers when needed, arrange data, execute the repository and assert results. Use the shared database cleanup helpers only on trusted test tables.

`SingletonMysqlHelper` can share a policy across tests; manage shared state and parallel cleanup carefully. Use a disposable test database, not production credentials.

See [Infrastructure.Persistence.Mysql](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Mysql), the [MySQL notes](https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html#mysql) and the [testing conventions](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html).

## Releases

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

- Updated the database testing and MySQL persistence dependencies alongside configurable maximum pool size support.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced MySQL integration test helpers, including a singleton variant and options mapper.
