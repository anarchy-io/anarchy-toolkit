# Local integration environment

Use Docker Compose V2. From the repository root, `make test_integration` starts the configured stack and waits for
PostgreSQL, MySQL, Oracle, and Moto to become healthy. If you run the Integration project directly (for example, in an
IDE or through `dotnet`), start the stack yourself and wait for its health checks first:

```bash
make start ARGS="--wait --wait-timeout 600"
dotnet run --project tests/Integration/Integration.csproj -c Release
```

The full Integration suite needs all three databases. If running only one provider's test class directly, append
its service name to `ARGS` to start only that service and filter the test run to that class. Moto backs the SNS/SQS
integration tests.

| Compose service | Host endpoint | Database / service | User | Password |
|---|---|---|---|---|
| `anarchy-db-postgres` | `localhost:5432` | `anarchy_test` | `anarchy_test` | `anarchy_test` |
| `anarchy-db-mysql` | `localhost:3306` | `anarchy_test` | `anarchy_test` | `anarchy_test` |
| `anarchy-db-mysql` (admin) | `localhost:3306` | — | `root` | `anarchy_test_root` |
| `anarchy-db-oracle` | `localhost:1521` | `FREEPDB1` | `anarchy_test` | `anarchy_test` |
| `anarchy-db-oracle` (admin) | `localhost:1521` | `FREEPDB1` | `SYSTEM` | `anarchy_test` |
| `anarchy-infra-aws` | `http://localhost:4566` | — | — | — |

These are disposable test credentials. Ports are published on the host and must be free. Do not run these tests against
shared or production databases: the tests modify known fixture rows in the default databases. They connect as
`anarchy_test`; no admin credentials are needed to run them. The stack cannot run alongside another environment using
the same ports.

## First startup and persistent data

- PostgreSQL and MySQL bind `db/{postgres,mysql}/init/` to `/docker-entrypoint-initdb.d` and import one DDL file during
  first initialization. Oracle binds `db/oracle/entrypoint/` to `/container-entrypoint-initdb.d`; its SQL*Plus hook loads
  `db/oracle/init/10-event-store.sql` from the mounted provider directory after switching to `FREEPDB1` and the
  `anarchy_test` schema. The provider scripts at `db/{postgres,mysql,oracle}/healthcheck.sh` verify fixture tables over
  TCP, without writing any rows.
- The tests use `test_order`, `type_registry`, and `domain_event` for the event-store scenario and `it_order`, `it_stock`,
  `it_event`, and `it_outbox` for the unit-of-work scenario. PostgreSQL uses `JSONB` and `TIMESTAMPTZ`; MySQL uses native
  `JSON` and stores UTC times in `DATETIME(6)`; Oracle uses native `JSON` and `TIMESTAMP WITH TIME ZONE`. Oracle's test
  adapter converts JSON to and from text in SQL so the common repository can keep using string parameters and results.
- Tests for each provider run in the same xUnit collection. Each scenario deletes only its known fixture rows at the
  start and leaves committed rows in place for inspection afterward. Don't run two test processes against the same
  stack concurrently or add unrelated rows to the fixture tables; the scenarios assert their contents.
- After `make test_integration`, inspect the retained state directly, for example:

  ```sh
  docker exec anarchy-db-postgres psql -U anarchy_test -d anarchy_test -c "SELECT id, aggregate_id, payload FROM domain_event ORDER BY id; SELECT * FROM it_outbox ORDER BY id;"
  docker exec -it anarchy-db-mysql mysql -u anarchy_test -p anarchy_test -e "SELECT id, aggregate_id, payload FROM domain_event ORDER BY id; SELECT * FROM it_outbox ORDER BY id;"
  docker exec -it anarchy-db-oracle sqlplus -s anarchy_test/anarchy_test@//localhost:1521/FREEPDB1
  ```

  In Oracle, query `domain_event` and `it_outbox` from the SQL*Plus prompt. The next test run clears only its known
  fixture rows before exercising them again; it does not drop the tables or volumes.
- These hooks aren't migrations. They run only on a fresh data volume; restarting with an existing volume won't add
  the new tables or change existing column types. A volume created with the earlier fixture can fail readiness because
  it lacks `it_outbox`. If initialization failed or the DDL changed, inspect `make container_logs` and preserve any data
  you need before using a fresh volume. `make stop` preserves the named database volumes; `make destroy` deletes them
  and their test data. Moto state is in memory.
- The pinned images provide Linux AMD64 and ARM64 builds. The Oracle image is a community Docker Hub image wrapping
  Oracle Database Free, whose binaries are governed by the
  [Oracle Free Use Terms and Conditions](https://www.oracle.com/downloads/licenses/oracle-free-license.html), not the
  toolkit's license. `slim-faststart` trades a larger image for faster initialization. Allow several minutes on first
  startup and give Docker enough RAM for the entire stack; the Oracle image warns below 2 GiB available to its container.
