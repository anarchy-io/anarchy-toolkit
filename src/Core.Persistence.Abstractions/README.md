# Anarchy.Toolkit.Core.Persistence.Abstractions

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Core · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Persistence contracts for applications that should not depend on a particular database provider.

```bash
dotnet add package Anarchy.Toolkit.Core.Persistence.Abstractions
```

- Use the unit-of-work and async-local flow-context contracts to coordinate persistence operations.
- Define repositories against the supplied repository and storage-policy abstractions and exceptions.
- Use `AttributeIdCacheBase<TAttributeTypes, TId>` for auxiliary IDs when a process-wide cache is safe, or `AttributeIdResolver<TAttributeTypes, TId>` for transaction-local lookups. IDs may be numeric, strings or GUIDs.
- Choose a concrete provider from the toolkit's relational adapters or implement your own.

## When to use it

Reference this package from application and domain-facing code that needs repository or transaction contracts but should not depend on a database driver. It defines boundaries, not a connection pool, SQL implementation or database schema.

## How to use it

1. Define application-specific repository interfaces and expose only the operations your use cases need. Implement them in an infrastructure project using the supplied reader, writer or batch contracts where appropriate.
2. Let a UseCase own a managed session with `RunInManagedSessionAsync`. An Aggregate that must also work alone can use `RunInJoinedOrManagedSessionAsync`; an Aggregate that only participates uses `RunInJoinedSessionAsync` and cannot commit or roll back the caller's transaction.
3. Use the storage-policy and flow-context contracts when implementing a provider adapter. Pass cancellation tokens through the operation so database work and rollback cleanup follow the unit-of-work contract.
4. In the host, register a concrete provider such as `Infrastructure.Persistence.Db.Postgres`, `Infrastructure.Persistence.Db.Oracle` or `Infrastructure.Persistence.Db.Mysql`. Supply connection and resilience options there, not in application code.

Select transaction boundaries deliberately: the abstractions do not provision databases, retry entire business operations or make external side effects transactional.

`AttributeIdCacheBase` shares results across a closed generic type, so use it only when the backing store identity and committed visibility are guaranteed. `AttributeIdResolver` does not cache or share identifiers: both callbacks must use the caller's intended transaction, and any returned uncommitted ID stays within that transaction. Concurrent first-time registration requires atomic storage get-or-insert and a uniqueness constraint.

See the [package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/) and [database provider guide](https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html#adding-a-database-provider).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
