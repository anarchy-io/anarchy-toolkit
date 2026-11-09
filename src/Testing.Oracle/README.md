# Anarchy.Toolkit.Testing.Oracle

Oracle integration-test helpers for the toolkit's ODP.NET persistence adapter.

```bash
dotnet add package Anarchy.Toolkit.Testing.Oracle
```

- Use `OracleHelper` or `SingletonOracleHelper` for connection, pool, resilience and unit-of-work fixtures.
- `OracleOptionsMapper` maps options; helpers include generic enum type-handler registration and table cleanup.
- Tests need an Oracle instance and a schema appropriate to the consuming application. Truncation uses plain `TRUNCATE TABLE`, without identity restart or cascade.

## When to use it

Use this package to test Oracle-specific SQL, mappings and persistence behavior against a real, isolated Oracle schema. It builds toolkit persistence fixtures but does not start Oracle or install your application schema.

## How to use it

1. Configure a test settings type implementing `IOracleAppSettings`; policy fixtures also require `IResilienceAppSettings`. Point it at a dedicated test instance and service name.
2. Call `OracleHelper.DbPolicyFixture<TAppSettings>()` or `OracleHelper.DbUnitOfWorkFixture<TAppSettings>()`, or pass a settings instance to the corresponding overload. Generic fixtures load embedded settings from the assembly declaring the settings type.
3. Use the policy or unit of work in your repository test. Register enum handlers where required and clean up known test tables with the shared helpers.

`SingletonOracleHelper` shares a policy when that fits the test lifecycle. Coordinate parallel tests and cleanup, and do not use production schemas or credentials.

See [Infrastructure.Persistence.Oracle](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Oracle), the [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) and the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.1.0

- Updated the database testing and Oracle persistence dependencies alongside configurable maximum pool size support.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced Oracle integration test helpers, including a singleton variant and options mapper.
