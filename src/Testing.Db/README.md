# Anarchy.Toolkit.Testing.Db

Provider-agnostic relational database helpers for integration tests.

```bash
dotnet add package Anarchy.Toolkit.Testing.Db
```

- Use `DbTestHelper.RegisterEnumTypeHandler<TEnum>()` to register enum type handlers.
- Use `TruncateTableAsync<TConnection, TDialect>` or `ClearTableAsync` for table cleanup and `DbOptionsMapper` for options mapping.
- Provider-specific fixtures are available in [Testing.Postgres](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Postgres), [Testing.Oracle](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Oracle) and [Testing.Mysql](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Mysql).

## When to use it

Use this package in database integration tests or when building a new provider's test kit. It contains common cleanup and mapping helpers, not a database server, schema migration tool or provider-specific connection setup.

## How to use it

Create a real test database and a provider-specific storage policy, then register any enum handlers before testing Dapper mappings. For cleanup, choose truncation through the dialect or a `DELETE` through `ClearTableAsync`:

```csharp
using Anarchy.Toolkit.Testing.Db.Helper;

DbTestHelper.RegisterEnumTypeHandler<OrderStatus>();
await DbTestHelper.ClearTableAsync<MyConnection>(policy, "orders", cancellationToken);
```

Replace `OrderStatus`, `MyConnection` and `policy` with your own enum, database connection type and configured policy. Table names are inserted into SQL, so use trusted fixed identifiers, never user input. Cleanup is destructive; use a dedicated test schema and account for foreign keys, identity behavior and tests running concurrently.

See the [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

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

- Updated the relational persistence dependency alongside configurable maximum pool size support.

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced provider-agnostic database test helpers and an options mapper.
