# Anarchy.Toolkit.Testing.Db.Oracle

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Testing kit · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Oracle integration-test helpers for the toolkit's ODP.NET persistence adapter.

```bash
dotnet add package Anarchy.Toolkit.Testing.Db.Oracle
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

See [Infrastructure.Persistence.Db.Oracle](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle), the [testing conventions](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html) and the [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
