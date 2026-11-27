# Naming

How to name namespaces, types and services so that a name says what the code is and who owns it. [Code Organization](code-organization.md) covers where code goes; this page covers what it is called.

![Architectural blueprint lines on a dark background with red-orange highlights, wide banner](https://anarchy-io.github.io/anarchy-toolkit/assets/images/headers/guides.png)

> **Recommendation, not a requirement.** The packages work without following this convention. It is a rule only for code in the toolkit repository (see [Contributing](https://anarchy-io.github.io/anarchy-toolkit/contributing/)); applications can adopt it, adapt it or skip it. [Opting out or adapting](#opting-out-or-adapting) explains how.

## Why

- **Names carry architecture.** A suffix or a namespace segment tells a reader the responsibility before they open the file. See [layered architecture](../decisions/0001-layered-architecture.md).
- **Consistency with .NET.** Plural namespaces and singular types match `System.Collections`, `System.Diagnostics`, `Microsoft.Extensions.Logging` and `Microsoft.AspNetCore.Mvc`.
- **No mechanical uniformity.** The rules keep names clear; they are not meant to rename everything.

## Namespaces

- Use **plural** nouns for namespace segments and **singular** nouns for types.
- Reflect architectural ownership and responsibility; avoid redundant segments.
- Keep namespaces and physical folders aligned, extension classes included (see [Extensions](code-organization.md#extensions)).
- Prefer responsibility-specific namespaces where they add clarity; keep `Helpers` for shared helper functions.
- Do not invent a namespace distinction only to avoid a name collision; give the type a more descriptive name instead.

```csharp
Acme.Core.Events.Domain
Acme.Core.Repositories
Acme.Infrastructure.Messaging
Acme.Bootstrap.Telemetry
```

Use `Exceptions`, `Extensions`, `Helpers`, `UseCases` and `Services.Aggregates` as appropriate.

## Type suffixes

Keep a suffix when it communicates an architectural distinction, and leave it off when it adds nothing.

| Suffix | Meaning | Example |
|---|---|---|
| `Entity` | Identity-bearing domain or persistence entity | `CustomerEntity` |
| `Model` | General-purpose representation | `CustomerModel` |
| `Dto` | Data transfer representation | `CustomerDto` |
| `Dao` | Data access object | `CustomerDao` |
| `Record` | Data-centric record or value representation | `DomainEventDbRecord` |
| `Options` | Configuration | `TelemetryOptions` |
| `Context` | Execution or operational context | `ApplicationContext` |
| `Result` | Operation outcome | `HealthCheckResult` |

- Do not use `Entity` as a synonym for "class". A transient message or a value is not an entity.
- Do not remove a representation suffix when it separates types with different responsibilities (`CustomerEntity` versus `CustomerDto`).
- Do not append a suffix mechanically when it adds no information.
- Prefer an explicit name when the namespace alone would leave a public API ambiguous.
- Keep names that are already clear: `IUnitOfWork`, `IStorageRepository`, `IMessageBrokerPublisher`.

## Services

Name a service implementation after what it does, with a responsibility-oriented agent noun, preferably ending in `-er` or `-or`: `Publisher`, `Worker`, `Masker`, `Parser`, `Serializer`, `Resolver`, `Dispatcher`, `Validator`.

- Avoid generic names such as `Manager` or `Processor` unless the responsibility is unambiguous.
- A service encapsulates one distinct operation or technical responsibility. Business workflows are [UseCases](usecases-and-aggregates.md#usecases) instead.
- Repositories always keep the `Repository` suffix; aggregates are services named `<Resource>Aggregate`.
- Helpers identify their subject or operation, for example `DateHelper`.

## Summary

| Concern | Convention |
|---|---|
| Namespaces | Plural nouns, aligned with folders |
| Types | Singular nouns |
| Services | Responsibility-oriented agent nouns (`Publisher`, `Validator`) |
| Repositories | `Repository` suffix |
| Entities | `Entity` suffix when semantically appropriate |
| DTOs, models, DAOs | `Dto`, `Model` and `Dao` suffixes to tell representations apart |
| Helpers | Named after their subject; pure helpers stay static and out of DI |
| Extensions | `Extensions` suffix, the subject's namespace |
| UseCases | `<Action><Subject>UseCase` |
| Aggregates | `<Resource>Aggregate` under `Services/Aggregates/` |

## Opting out or adapting

| You want to... | Do this |
|---|---|
| Keep singular namespaces or your own suffixes | Keep them within your solution and write them down in an ADR or `AGENTS.md`; nothing in the toolkit reads names. |
| Keep `Service` or `Entity` folders | Fine inside your own projects; nothing in the toolkit requires them to change. |

**What you lose by opting out:** names that match the toolkit's and .NET's own, so mixed code reads less uniformly and agents need your documented exceptions to follow your style.
