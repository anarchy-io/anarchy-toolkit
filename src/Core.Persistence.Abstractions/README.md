# Anarchy.Toolkit.Core.Persistence.Abstractions

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
2. Coordinate related writes through the unit-of-work abstraction, and use the storage-policy and flow-context contracts when implementing an adapter.
3. In the host, register a concrete provider such as `Infrastructure.Persistence.Postgres`, `Infrastructure.Persistence.Oracle` or `Infrastructure.Persistence.Mysql`. Supply connection and resilience options there, not in application code.

Select transaction boundaries deliberately: the abstractions do not provision databases, retry entire business operations or make external side effects transactional.

See the [package catalogue](https://github.com/anarchy-io/anarchy-toolkit#package-catalogue) and [database provider guide](https://github.com/anarchy-io/anarchy-toolkit#adding-a-database-provider).

## Releases

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced repository and unit-of-work contracts, storage policies, flow context and persistence exceptions.
