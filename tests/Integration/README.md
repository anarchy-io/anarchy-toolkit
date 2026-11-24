# Integration tests

Database tests are skipped unless `ANARCHY_RUN_DATABASE_TESTS=true`. Start the desired profile in
`Environment/docker-compose.yml` and select its test class (`*PostgresDbUnitOfWorkIntegrationTest`,
`*MysqlDbUnitOfWorkIntegrationTest`, or `*OracleDbUnitOfWorkIntegrationTest`) when only one provider is running.
If the whole integration project is run with the flag enabled, all three databases must be available.

The provider tests use the localhost ports and credentials in the Compose file. The event-store repository scenario creates
an isolated, randomly named PostgreSQL schema, MySQL database or Oracle user/schema for each invocation, and drops only
that namespace in `finally`. MySQL uses the Compose root account to create the database; Oracle uses the Compose
`SYSTEM` account to create and grant privileges to the temporary user. These tests are for the disposable Compose
environment, not shared or production databases. If the process is killed before cleanup, an `at_`-prefixed namespace
may remain and must be inspected before manual removal. Oracle DDL is executed outside transactions.

The event-store fixture exercises the toolkit repository bases, concurrent type registration, custom-attribute round
trips, and atomic writes across the order and event repositories. Its SQL, uniqueness constraint, and single type
registry are test fixtures, not production schema recommendations.

Separately, the unit-of-work scenario uses plain SQL against a test-owned outbox to show how an application can include
its own outbox in a transaction. No broker is involved: marking a row published does not prove message delivery or
consumer deduplication.
