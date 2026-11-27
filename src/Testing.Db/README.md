# Anarchy.Toolkit.Testing.Db

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Testing kit · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Provider-agnostic relational database helpers for integration tests.

```bash
dotnet add package Anarchy.Toolkit.Testing.Db
```

- Use `DbTestHelper.RegisterEnumTypeHandler<TEnum>()` to register enum type handlers.
- Use `TruncateTableAsync<TConnection, TDialect>` or `ClearTableAsync` for table cleanup and `DbOptionsMapper` for options mapping.
- Provider-specific fixtures are available in [Testing.Db.Postgres](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Db.Postgres), [Testing.Db.Oracle](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Db.Oracle) and [Testing.Db.Mysql](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Db.Mysql).

## When to use it

Use this package in database integration tests or when building a new provider's test kit. It contains common cleanup and mapping helpers, not a database server, schema migration tool or provider-specific connection setup.

## How to use it

Create a real test database and a provider-specific storage policy, then register any enum handlers before testing Dapper mappings. For cleanup, choose truncation through the dialect or a `DELETE` through `ClearTableAsync`:

```csharp
using Anarchy.Toolkit.Testing.Db.Helpers.Database;

DbTestHelper.RegisterEnumTypeHandler<UserStatus>();
await DbTestHelper.ClearTableAsync<MyConnection>(policy, "users", cancellationToken);
```

Replace `UserStatus`, `MyConnection` and `policy` with your own enum, database connection type and configured policy. Table names are inserted into SQL, so use trusted fixed identifiers, never user input. Cleanup is destructive; use a dedicated test schema and account for foreign keys, identity behavior and tests running concurrently.

See the [testing conventions](https://anarchy-io.github.io/anarchy-toolkit/conventions/testing.html) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [Dapper](https://www.nuget.org/packages/Dapper) | Registers enum type handlers and runs the cleanup SQL | No |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
