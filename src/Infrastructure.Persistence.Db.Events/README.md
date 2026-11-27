# Anarchy.Toolkit.Infrastructure.Persistence.Db.Events

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Infrastructure · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Provider-agnostic repository contracts and bases for a relational domain event store, and contracts for a transactional task outbox.
This package builds on `Infrastructure.Persistence.Db` and `Core.Events`. Applications own their schema and supply
concrete repositories and provider-specific embedded SQL.

```bash
dotnet add package Anarchy.Toolkit.Infrastructure.Persistence.Db.Events
```

## How to use it

- Derive domain event writers from `DbEventWriterRepositoryBase` and readers from `DbDomainEventReaderRepositoryBase`.
- Implement the outbox reader and writer contracts in the application. Save tasks in the same unit-of-work transaction
  as the business changes, then use a relay to publish the pending tasks.
- Ship the `DomainEvent.Get.*` and auxiliary type-table queries described on the event repository bases.
  Type identifiers are resolved against the caller's database and active transaction on every lookup; no identifier is
  shared across lookups. The provider's get-or-insert SQL and database unique constraints coordinate concurrent
  registration. A rollback handler is not required for this resolution path.

## SQL contract for custom attributes

`IEventEnvelope.CustomAttributes` is stored separately from event data as a JSON object with string values. Add a
text/JSON column (for example `custom_attributes`) to the event table. The column may be nullable:
a SQL `NULL` is read as an empty attribute dictionary.

- The concrete domain event writer must serialize `CustomAttributes` and bind the JSON text in its insert query.
- Every event query returning a `DomainEventDbRecord` must select the column as a
  string aliased `CustomAttributes`. Cast a native JSON column to text where the driver requires it.
- Outbox implementations define their own schema, serialization, duplicate handling, retention and cleanup policy.

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [Dapper](https://www.nuget.org/packages/Dapper) | Runs the event-store repositories' SQL | No |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
