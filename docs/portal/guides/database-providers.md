# Database Providers

This guide explains how the toolkit's relational persistence is split between a provider-agnostic core and thin provider adapters, what is specific to each supported database, and how to add a new one.

## How the layers fit together

| Package | Role |
|---|---|
| [`Core.Persistence.Abstractions`](../packages/Core.Persistence.Abstractions/README.md) | Unit-of-work, flow-context, storage-policy and repository contracts. No database driver. |
| [`Infrastructure.Persistence.Db`](../packages/Infrastructure.Persistence.Db/README.md) | Everything that is not provider-specific: the `IDbDialect` contract, unit of work, connection pool, resilience, repository bases and health reports. It contains no business-event or outbox code; it references `Core.Events` only for generic transaction lifecycle notifications on the local event bus. |
| [`Infrastructure.Persistence.Db.Events`](../packages/Infrastructure.Persistence.Db.Events/README.md) | Event-store database bases and Outbox contracts and records, including `RolledBackTransactionEventHandler` in its service namespace. Applications implement Outbox repositories. It builds on the generic bases and is optional; see [ADR 0007](../decisions/0007-events-persistence-package.md). |
| `Infrastructure.Persistence.Db.<Provider>` | A thin adapter closing the generic bases over the provider's connection and dialect. |
| [`Testing.Db`](../packages/Testing.Db/README.md) and `Testing.Db.<Provider>` | Test helpers, with the same split. |

Applications reference a provider package and register its module; their own code only depends on the abstractions and the repository bases.

## Supported providers

### PostgreSQL

[`Infrastructure.Persistence.Db.Postgres`](../packages/Infrastructure.Persistence.Db.Postgres/README.md) uses Npgsql. Register `ModulePostgres(connectionOptions, resilienceOptions)` and, for health checks, `ModulePostgresHealthReport` (or `AddPostgresHealthReports()`). Test with [`Testing.Db.Postgres`](../packages/Testing.Db.Postgres/README.md).

### Oracle

[`Infrastructure.Persistence.Db.Oracle`](../packages/Infrastructure.Persistence.Db.Oracle/README.md) uses ODP.NET (`Oracle.ManagedDataAccess.Core`). Register `ModuleOracle(connectionOptions, resilienceOptions)` and `ModuleOracleHealthReport`. `OracleConnectionOptions.DbName` is the service name, and the port defaults to 1521. The connection string is a full connect descriptor with an `EXPIRE_TIME` keepalive. Health checks run `SELECT SYSDATE FROM DUAL`, with tests named `oracle_connection-{reader|writer}`. Test with [`Testing.Db.Oracle`](../packages/Testing.Db.Oracle/README.md).

### MySQL

[`Infrastructure.Persistence.Db.Mysql`](../packages/Infrastructure.Persistence.Db.Mysql/README.md) uses MySqlConnector. Register `ModuleMysql` with factories for `MysqlConnectionOptions` and `MysqlResilienceOptions`; `ModuleMysqlHealthReport` (or `AddMysqlHealthReports()`) registers the reader and writer checks. The unkeyed `IStoragePolicy<MySqlConnection>` uses the writer; use the `reader` key for read-only operations.

- Hosts may be absolute URIs such as `tcp://mysql.example` or relative host names. The port defaults to 3306, and the command timeout is in seconds.
- A nonempty `TimeZone` is applied to each newly opened session. Named zones such as `UTC` require populated MySQL time-zone tables; offsets such as `+00:00` do not.
- The adapter enables MySqlConnector's `Ignore Command Transaction` option, because the shared repositories execute commands on the unit of work's connection without passing its transaction explicitly.
- Individual commands are not retried inside a transaction: a deadlock may invalidate the entire transaction, so retry the whole unit of work instead.
- Batch writes use Dapper's execute-many.

Test with [`Testing.Db.Mysql`](../packages/Testing.Db.Mysql/README.md), which provides `IMysqlAppSettings` in `Anarchy.Toolkit.Testing.Db.Mysql.Entity.Configuration` for the settings-based fixtures. Table truncation resets auto-increment and may fail on tables referenced by foreign keys.

## Provider notes

Provide SQL written for the target database for your repositories and outbox implementations; the toolkit does not translate it.

**Oracle SQL.** SQL files must not end with `;` (ODP.NET rejects it) and use `:Name` binds (Dapper turns on `BindByName`). Since Oracle cannot select from an `INSERT` (`IDbDialect.CanSelectFromInsert`), event writers use `Common.<Type>.Insert.IfMissing`, then `Common.<Type>.Get.ByName` to resolve type IDs. Cast IDs with `CAST(… AS NUMBER(9|18))` so ODP.NET returns `int`/`long`. Savepoints are never released, as Oracle has no such statement, and `TruncateTableAsync` uses a plain `TRUNCATE TABLE` (no identity restart or cascade).

## Adding a database provider

A new provider only adds a thin adapter, mirroring the existing provider projects:

1. **`Infrastructure.Persistence.Db.<Provider>`**:
   - a dialect implementing `IDbDialect`: `Name`, `IsNoData` and `IsUniqueViolation`, plus overrides of the ANSI defaults that differ (`IsServerError`, `IsTransient`, the savepoint SQL, `ToBatchParameters`, `HealthCheckSql`, `TruncateTableSql`). Return `null` from `SavepointReleaseSql` when the provider has no `RELEASE SAVEPOINT`;
   - a connection string (`DbConnectionStringBase<TOptions>`) and a pool (`DbConnectionPoolBase<TConnection>.CreateConnection`);
   - one-line unit-of-work, policy, retry-builder and repository-base subclasses closing the generics over the provider's connection and dialect;
   - a `Module<Provider>` under `DependencyInjection/Module/`.
2. **Health, in the same project**: reader and writer subclasses of `DbHealthReportBase`, an `Add<Provider>HealthReports()` extension and a `Module<Provider>HealthReport`.
3. **`Testing.Db.<Provider>`**: helpers delegating to `DbTestHelper`.
4. **Events and the outbox**: `Infrastructure.Persistence.Db.Events` supplies event-store reader/writer bases and Outbox contracts. Applications provide concrete event repositories and provider SQL in a `Queries/` folder next to them (`LoadSql` resolves from the most derived type's namespace), and implement their own Outbox repositories and schema.

Then run `make check_layering`: provider projects belong to the `Infrastructure` layer and must not reference any higher layer.
