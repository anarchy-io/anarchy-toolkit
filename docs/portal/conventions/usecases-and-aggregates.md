# UseCases and Aggregates

How business workflows and multi-entity coordination are structured, and who owns the transaction when they meet. [Code Organization](code-organization.md) says where these types live; [Naming](naming.md) how they are named.

![Architectural blueprint lines on a dark background with red-orange highlights, wide banner](../assets/images/headers/guides.png)

> **Recommendation, not a requirement.** The packages work without following this convention. It is a rule only for code in the toolkit repository (see [Contributing](../contributing/README.md)); applications can adopt it, adapt it or skip it. [Opting out or adapting](#opting-out-or-adapting) explains how.
{: .notice--info}

## Why

- **Readable workflows.** A UseCase reads as the recipe for a business operation, so someone who knows the organization can follow it.
- **Coordination in one place.** An aggregate keeps related entities consistent, so repositories stay about persistence access.
- **Explicit transactions.** Saying who owns the transaction prevents a participant from committing what it does not own.

The [`Samples.Examples`](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Examples) project runs a complete example (`dotnet run --project samples/Samples.Examples -- orders`).

## UseCases

A UseCase is a business workflow. It lives in a project-root `UseCases/` folder and namespace and reads as the step-by-step recipe a person would follow in the organization:

```text
UseCases/
└── Users/
    └── UpdateUserUseCase.cs
```

```csharp
public sealed class UpdateUserUseCase(IUserRepository userRepository, IUserValidator userValidator)
{
    public async Task<UserEntity> ExecuteAsync(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        user.Update(request);

        await userValidator.ValidateAsync(user, cancellationToken);
        await userRepository.SaveAsync(user, cancellationToken);

        return user;
    }
}
```

The example is illustrative: where request-to-domain mapping and validation happen depends on your application boundary.

- A UseCase does not implement an interface by default, and exposes **one public execution method**.
- Avoid private helper methods; extract independent or technical responsibilities into injected dependencies, so the class stays a readable sequence of business operations.
- Prefer explicit orchestration over generic abstractions that hide the workflow.
- A UseCase may coordinate repositories, services, aggregates and transactions. It is not the place for unrelated business rules or infrastructure details.
- Callers (a controller after request validation and mapping, a job handler) call the UseCase and return its result.

A useful test: a non-technical person who knows the organization's operations should be able to read the UseCase and understand the sequence of actions. It is a design guideline, not a mechanical rule, and not every context benefits from explicit UseCases; consider them each time.

## Aggregates

An aggregate coordinates the lifecycle and persistence of related entities that together represent one business or application resource. Aggregates are a type of service: they live under `Services/Aggregates/` in the `Services.Aggregates` namespace.

Introduce one when a resource consists of several independently represented entities that must be loaded, changed and persisted together. For example, a `CustomerEntity` with `AddressEntity` items and a `ContactInfoEntity`, each in its own table with its own repository, is coordinated by a `CustomerAggregate`:

```text
Services/Aggregates/Customers/CustomerAggregate.cs

CustomerAggregate
├── CustomerRepository
├── AddressRepository
└── ContactInfoRepository
```

An aggregate:

- loads and assembles the related entities;
- coordinates changes and writes across them, delegating each individual write to its repository;
- keeps the composed resource consistent;
- manages transaction boundaries where appropriate (see below);
- is a natural place to coordinate domain-event persistence and the transactional outbox.

Not every group of related entities needs an aggregate: add one when coordination, consistency or transactions justify it.

### Repository or aggregate?

Repositories own persistence access and querying. A repository may query several tables when that is right for performance and does not stretch its responsibility, but writing to several tables as one coordinated operation is generally the aggregate's job.

| Responsibility | Repository | Aggregate |
|---|---|---|
| Query one or several tables | Yes | Through repositories |
| Persist one entity | Yes | Through repositories |
| Assemble related entities | May provide query projections | Yes |
| Coordinate several entity writes | Generally not | Yes |
| Keep related entities consistent | Generally not | Yes |
| Coordinate a transaction across repositories | Generally not | Yes |
| Coordinate transactional outbox creation | Generally not | Yes |

## Transaction ownership

The code that **starts** a transaction owns it; everyone else participates. Make the contract explicit, in one of three arrangements:

1. **Aggregate-owned.** The aggregate starts and completes its own transaction.
2. **Nested or savepoint.** The aggregate reuses an existing transaction, and may use savepoints where the database supports them.
3. **Caller-owned.** The aggregate participates in a transaction whose lifecycle a UseCase (or another caller) controls.

An aggregate must never commit, roll back or dispose a transaction its caller owns. With the toolkit's `IUnitOfWork` (`Core.Persistence.Abstractions`):

- A UseCase that owns the transaction runs the work in a managed session: `RunInManagedSessionAsync`.
- An aggregate that must work both alone and inside a caller's transaction uses `RunInJoinedOrManagedSessionAsync`: it joins the caller's transaction when there is one and otherwise owns a managed session.
- An aggregate that only ever participates uses `RunInJoinedSessionAsync`; it cannot begin, commit or roll back (`TransactionNotOwnedException`).
- An exception escaping the owner's managed callback rolls its transaction back; a joined callback does not roll back by itself. If the owner catches an exception and continues, it is responsible for ensuring the transaction is still valid and the partial changes are acceptable. Cancellation propagates after rollback and cleanup. The unit of work rejects nested and concurrent sessions.

```csharp
// The UseCase owns the transaction…
var order = await unitOfWork.RunInManagedSessionAsync(
    async token => await aggregate.PlaceAsync(input, token), cancellationToken);

// …and the aggregate joins it (or owns a session when called alone).
public Task<Order> PlaceAsync(PlaceOrderInput input, CancellationToken cancellationToken) =>
    unitOfWork.RunInJoinedOrManagedSessionAsync(async token => { /* repositories, outbox */ }, cancellationToken);
```

The package details (failure data, cleanup, connection handling) are in the [`Infrastructure.Persistence.Db` README](../packages/Infrastructure.Persistence.Db/README.md).

### Coordinating repositories and events

All writes that must succeed together—including the entity, its related entities, the domain-event record and the
outbox task—must use the **same database transaction**. Configure the unit of work and every participating writer
with the same writer connection pool and `IAsyncFlowContext`; a second database, an independently opened connection,
an ad-hoc session or a message broker does not enlist automatically. Run the writes sequentially in the session and
let failures escape to its owner. Do not publish to the broker inside the transaction: a separate relay publishes
committed outbox rows and marks them published afterwards. The sample's in-memory stores illustrate the orchestration,
not database isolation or durability.

The relational events package provides outbox repository contracts, not concrete outbox repositories, an application
event-store writer, schema, provider SQL or a coordinator that automatically enlists them. Supply those pieces and persist the same logical event in both stores
inside the aggregate's session. A unique event or operation ID enforced by the database is preferable to treating a
payload hash as a universal idempotency key: identical payloads can represent distinct occurrences, and retries must
reuse the original ID. Decide whether duplicate insertion is an error or a no-op in the application's SQL and test it.
Outbox delivery is normally **at least once**: a relay can crash after publishing and before marking the row. Consumers
must deduplicate using a stable message ID. Concurrent relays also need an application-specific claiming strategy.

`IUnitOfWork` does not expose nested transactions or aggregate-level savepoints. The relational batch loop uses a
savepoint per insert only while a transaction is active; it can report duplicates after other inserts have succeeded.
Let that exception reach the owner to roll back the entire operation, unless the owner explicitly accepts partial
success. A custom savepoint arrangement must use the same connection, respect provider limitations (Oracle does not
release savepoints), and only catch failures for which the provider leaves the transaction usable. It does not transfer
ownership of the outer transaction to the aggregate. Without a transaction, individual batch-loop writes can commit
before a later failure.

Cleanup is application policy: only delete published rows, keep them long enough for recovery, auditing and consumer
deduplication, and do not delete pending rows. Define and test retention and cleanup in the application-specific
outbox implementation. Database-level retention does not itself guarantee delivery.

## Opting out or adapting

| You want to... | Do this |
|---|---|
| Skip UseCases | Call services from controllers and handlers; keep orchestration in application services. Introduce UseCases where a workflow becomes hard to read. |
| Skip aggregates | Keep multi-entity writes in a service that owns a managed session, or in a single repository where it is simple and contained. |
| Put UseCases behind interfaces | Do so where you need a seam; the convention only says not to by default. |
| Manage transactions elsewhere | Use your own unit of work; keep the ownership rule: the starter completes. |

**What you lose by opting out:** the shared vocabulary for where workflows and multi-entity writes live, and the explicit transaction contract that keeps participants from committing a caller's transaction.
