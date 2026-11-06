# Anarchy.Toolkit.Infrastructure.Persistence.Db

Provider-agnostic relational persistence infrastructure for toolkit database adapters.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Db
```

- `IDbDialect`, connection-pool and options bases, unit of work, resilience policy and retry builder provide the common database layer.
- Build repositories on the reader, writer and batch-writer bases; database health reports use the dialect's health-check SQL.
- Transactional outbox and event-store contracts, records and repository bases are included. Consumers supply provider-specific concrete repositories and SQL.
- Use a provider adapter for PostgreSQL, Oracle or MySQL, or implement a new dialect and adapter.

See [adding a database provider](https://github.com/anarchy-io/anarchy-toolkit#adding-a-database-provider) and the [persistence notes](https://github.com/anarchy-io/anarchy-toolkit#notes--follow-ups), including known limitations.
