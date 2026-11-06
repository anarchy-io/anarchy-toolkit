# Anarchy.Toolkit.Infrastructure.Persistence.Mysql

MySQL adapter for the toolkit's relational persistence layer, using MySqlConnector.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Mysql
```

- `MysqlDbDialect`, connection pools, unit of work, retry policy and repository bases adapt the shared relational layer to MySQL.
- Register an `Observer` in DI and configure `ModuleMysql` with connection and resilience options factories; `AddMysqlHealthReports()` adds reader and writer checks.
- The unkeyed storage policy uses the writer; use the `reader` key for read-only operations. Port 3306 is the default.
- Named session time zones require populated MySQL time-zone tables. Retry the whole unit of work rather than individual commands inside a transaction.

See the [MySQL usage notes](https://github.com/anarchy-io/anarchy-toolkit#mysql) and [Testing.Mysql](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Mysql).
