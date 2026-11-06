# Anarchy.Toolkit.Testing.Db

Provider-agnostic relational database helpers for integration tests.

```bash
dotnet add package Anarchy.Toolkit.Testing.Db
```

- Use `DbTestHelper.RegisterEnumTypeHandler<TEnum>()` to register enum type handlers.
- Use `TruncateTableAsync<TConnection, TDialect>` or `ClearTableAsync` for table cleanup and `DbOptionsMapper` for options mapping.
- Provider-specific fixtures are available in [Testing.Postgres](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Postgres), [Testing.Oracle](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Oracle) and [Testing.Mysql](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Mysql).

See the [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
