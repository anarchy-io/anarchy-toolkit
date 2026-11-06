# Anarchy.Toolkit.Testing.Mysql

MySQL integration-test helpers for the toolkit's MySqlConnector persistence adapter.

```bash
dotnet add package Anarchy.Toolkit.Testing.Mysql
```

- Use `MysqlHelper` or `SingletonMysqlHelper` for database fixtures and `MysqlOptionsMapper` for options mapping.
- `IMysqlAppSettings` supports settings-based fixtures; helpers also provide enum handlers and table cleanup.
- Tests need a MySQL instance and an application schema. MySQL `TRUNCATE` resets auto-increment and may fail for referenced tables.

See [Infrastructure.Persistence.Mysql](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Mysql), the [MySQL notes](https://github.com/anarchy-io/anarchy-toolkit#mysql) and the [testing conventions](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/testing-conventions.md).
