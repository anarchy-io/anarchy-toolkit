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
- A failure thrown inside the session rolls everything back at the owner, and cancellation propagates after rollback and cleanup. The unit of work rejects nested and concurrent sessions.

```csharp
// The UseCase owns the transaction…
var order = await unitOfWork.RunInManagedSessionAsync(
    async token => await aggregate.PlaceAsync(input, token), cancellationToken);

// …and the aggregate joins it (or owns a session when called alone).
public Task<Order> PlaceAsync(PlaceOrderInput input, CancellationToken cancellationToken) =>
    unitOfWork.RunInJoinedOrManagedSessionAsync(async token => { /* repositories, outbox */ }, cancellationToken);
```

The package details (failure data, cleanup, connection handling) are in the [`Infrastructure.Persistence.Db` README](../packages/Infrastructure.Persistence.Db/README.md).

## Opting out or adapting

| You want to... | Do this |
|---|---|
| Skip UseCases | Call services from controllers and handlers; keep orchestration in application services. Introduce UseCases where a workflow becomes hard to read. |
| Skip aggregates | Keep multi-entity writes in a service that owns a managed session, or in a single repository where it is simple and contained. |
| Put UseCases behind interfaces | Do so where you need a seam; the convention only says not to by default. |
| Manage transactions elsewhere | Use your own unit of work; keep the ownership rule: the starter completes. |

**What you lose by opting out:** the shared vocabulary for where workflows and multi-entity writes live, and the explicit transaction contract that keeps participants from committing a caller's transaction.
