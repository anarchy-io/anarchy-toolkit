# Anarchy.Toolkit.Core

Foundational, infrastructure-independent building blocks for .NET services.

```bash
dotnet add package Anarchy.Toolkit.Core
```

- Use helpers and extensions for strings, enums, dates and JSON, plus common exception primitives.
- Mark domain objects with `IEntity`, `IPersistedEntity` or `IDeepCloneableEntity` as appropriate.
- Implement `IDependencyInjectionModule` and register it with `UseModule<T>()` or `UseModule(module)`; use the keyed client-resolver abstractions when selecting clients by key.

See the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme) and [dependency-injection guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/architecture.md#dependency-injection-modules).
