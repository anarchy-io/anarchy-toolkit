# 0011. Event and outbox persistence in a separate package

## Context

`Infrastructure.Persistence.Db` held both generic relational infrastructure and event/outbox classes. Applications that need only a unit of work and repositories therefore took business-event and outbox code. The attribute-ID cache was also in the database package although it is not tied to any driver or domain.

## Decision

- Event-store database bases and Outbox contracts and records live in `Infrastructure.Persistence.Db.Events`, which depends on `Infrastructure.Persistence.Db` and `Core.Events`. Applications implement their own Outbox repositories. `RolledBackTransactionEventHandler` is in a service namespace of that package.
- `Infrastructure.Persistence.Db` stays generic and business-neutral and does not reference the events package. It keeps its `Core.Events` reference for generic transaction lifecycle notifications on the local event bus; that is infrastructure plumbing, not business-event coupling.
- The generic `AttributeIdCacheBase` is in `Core.Persistence.Abstractions.Model` and supports `string` and `Guid` identifiers as well as numeric ones.
- Event envelope names follow CloudEvents terms: `AggregateType`, `Subject`, `Type`, `Time`, `Data` and `CustomAttributes`. The CloudEvents mapper in `Infrastructure.Messaging.CloudEvents` is injected and each property mapping can be overridden; it interoperates with AWS SNS/SQS representations.

## Consequences

- Applications without business events or an outbox do not reference event storage (they still bring `Core.Events` through the generic Db package); the package name hierarchy and layering checks enforce the direction of references.
- **Cache safety.** A process-wide cache can expose an ID inserted by a transaction that later rolls back, or mix databases. Event writers use the non-sharing `AttributeIdResolver`, which resolves IDs on each call in the caller's transaction; no entries are shared across transactions or databases. Concurrent registration relies on storage-enforced uniqueness and atomic get-or-insert. Applications may use the separate `AttributeIdCacheBase` only where committed visibility and isolation are guaranteed.
- CloudEvents mapping does not promise lossless round-trips of metadata, and neither it nor the outbox provides delivery guarantees: durability, retries, ordering and deduplication are designed per transport and application.
- Packages that introduce this split start at `1.0.0`; changes to existing public contracts are breaking and follow the release tooling.

## Alternatives considered

- **Keep events in `Infrastructure.Persistence.Db`.** Fewer packages, but the generic layer stays coupled to events.
- **One events package per provider.** Duplicates logic already expressed over the generic bases and dialect contract.
