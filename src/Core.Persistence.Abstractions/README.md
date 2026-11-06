# Anarchy.Toolkit.Core.Persistence.Abstractions

Persistence contracts for applications that should not depend on a particular database provider.

```bash
dotnet add package Anarchy.Toolkit.Core.Persistence.Abstractions
```

- Use the unit-of-work and async-local flow-context contracts to coordinate persistence operations.
- Define repositories against the supplied repository and storage-policy abstractions and exceptions.
- Choose a concrete provider from the toolkit's relational adapters or implement your own.

See the [package catalogue](https://github.com/anarchy-io/anarchy-toolkit#package-catalogue) and [database provider guide](https://github.com/anarchy-io/anarchy-toolkit#adding-a-database-provider).
