# Integration tests

The regular Integration suite includes PostgreSQL, MySQL, Oracle, database health endpoint, and Moto-backed SNS/SQS
tests. `make test_integration` starts the local stack and waits for service health. Before running the project directly,
start the [local database and Moto stack](../../docker/README.md) and wait for readiness. To run one provider's tests,
select its class (`*PostgresDbUnitOfWorkIntegrationTest`, `*MysqlDbUnitOfWorkIntegrationTest`, or
`*OracleDbUnitOfWorkIntegrationTest`) with the runner's `-class` filter.

The provider tests use the localhost ports, credentials, and pre-initialized tables in the Compose file. Tests serialize
per provider, clear only their known fixture rows at the start, and leave the final committed rows in the default test
database for inspection after the run. Do not run two Integration processes against the same stack, or use these
credentials against shared or production databases. Oracle DDL is installed by the container's SQL*Plus startup hook.

The event-store fixture exercises the toolkit repository bases, concurrent type registration, custom-attribute round
trips, and atomic writes across the order and event repositories. The provider DDL, uniqueness constraints, and single
type registry are test fixtures that intentionally use provider-specific SQL; they are not production schema
recommendations.

Separately, the unit-of-work scenario uses the pre-initialized test-owned outbox to show how an application can include
its own outbox in a transaction. No broker is involved there: marking a row published does not prove message delivery or
consumer deduplication. The SNS/SQS tests use Moto and the `Testing.Aws` helpers; each chooses its own isolated or shared
resource lifecycle and cleans up resources it creates.
