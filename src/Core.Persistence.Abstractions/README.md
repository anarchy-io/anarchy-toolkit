# Anarchy.Toolkit.Core.Persistence.Abstractions

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Core · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Persistence contracts for applications that should not depend on a particular database provider.

```bash
dotnet add package Anarchy.Toolkit.Core.Persistence.Abstractions
```

- Use the unit-of-work and async-local flow-context contracts to coordinate persistence operations.
- Define repositories against the supplied repository and storage-policy abstractions and exceptions.
- Choose a concrete provider from the toolkit's relational adapters or implement your own.

## When to use it

Reference this package from application and domain-facing code that needs repository or transaction contracts but should not depend on a database driver. It defines boundaries, not a connection pool, SQL implementation or database schema.

## How to use it

1. Define application-specific repository interfaces and expose only the operations your use cases need. Implement them in an infrastructure project using the supplied reader, writer or batch contracts where appropriate.
2. Let a UseCase own a managed session with `RunInManagedSessionAsync`. An Aggregate that must also work alone can use `RunInJoinedOrManagedSessionAsync`; an Aggregate that only participates uses `RunInJoinedSessionAsync` and cannot commit or roll back the caller's transaction.
3. Use the storage-policy and flow-context contracts when implementing a provider adapter. Pass cancellation tokens through the operation so database work and rollback cleanup follow the unit-of-work contract.
3. In the host, register a concrete provider such as `Infrastructure.Persistence.Db.Postgres`, `Infrastructure.Persistence.Db.Oracle` or `Infrastructure.Persistence.Db.Mysql`. Supply connection and resilience options there, not in application code.

Select transaction boundaries deliberately: the abstractions do not provision databases, retry entire business operations or make external side effects transactional.

See the [package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/) and [database provider guide](https://anarchy-io.github.io/anarchy-toolkit/guides/database-providers.html#adding-a-database-provider).

## Releases

### 1.1.1

- docs(packages): identify each package's pillar and layer
- build(packages): attribute the copyright to Leandro Melão Medeiros
- build: relicense under Apache-2.0 and update the package metadata

### 1.1.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1

### 1.0.6

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.0.5

- refactor(src): clean up code style across library packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced repository and unit-of-work contracts, storage policies, flow context and persistence exceptions.
