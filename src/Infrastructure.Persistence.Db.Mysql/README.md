# Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

MySQL adapter for the toolkit's relational persistence layer, using MySqlConnector.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql
```

- `MysqlDbDialect`, connection pools, unit of work, retry policy and repository bases adapt the shared relational layer to MySQL.
- Register `AppTelemetry` in DI and configure `ModuleMysql` with connection and resilience options factories; `AddMysqlHealthReports()` adds reader and writer checks.
- The unkeyed storage policy uses the writer; use the `reader` key for read-only operations. Port 3306 is the default.
- Named session time zones require populated MySQL time-zone tables. Retry the whole unit of work rather than individual commands inside a transaction.

## When to use it

Use this adapter for a MySQL-backed application that needs the toolkit's relational policies, unit of work and repository bases. The consuming application supplies its schema, migrations and MySQL-specific queries.

## How to use it

1. Register `AppTelemetry` and use `ModuleMysql` with factories for `MysqlConnectionOptions` and `MysqlResilienceOptions`. Obtain hosts and credentials from the environment or a secret provider rather than committing them.
2. Configure reader/writer hosts, database name, command timeout and optional session time zone. The port defaults to 3306 and `MaximumPoolSize` defaults to 100; tune pool size for the database and number of application instances.
3. Implement repositories with the MySQL bases and select the keyed `reader` policy for read-only work. The unkeyed policy uses the writer. Add `AddMysqlHealthReports()` if the host should check both endpoints.
4. Validate the integration against a dedicated MySQL test database using `Testing.Db.Mysql`. If a transaction fails, retry the whole unit of work rather than individual statements inside it.

See the [MySQL usage notes](https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html#mysql) and [Testing.Db.Mysql](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Db.Mysql).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
