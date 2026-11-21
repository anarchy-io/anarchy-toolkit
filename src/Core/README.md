# Anarchy.Toolkit.Core

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Core · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Foundational, infrastructure-independent building blocks for .NET services.

```bash
dotnet add package Anarchy.Toolkit.Core
```

- Use helpers and extensions for strings, enums, dates and JSON, plus common exception primitives.
- Mark domain objects with `IEntity`, `IPersistedEntity` or `IDeepCloneableEntity<T>` as appropriate.
- Implement `IDependencyInjectionModule` and register it with `UseModule<T>()` or `UseModule(module)`; use the keyed client-resolver abstractions when selecting clients by key.

## When to use it

Reference Core from domain and application projects that need shared contracts without choosing a database, transport or hosting model. Use the more specific `Core.*` packages only when their corresponding concern is needed; concrete adapters belong at the application's composition root.

## How to use it

1. Implement the entity interfaces only where their contracts fit your domain model. `IPersistedEntity` identifies persisted entities, while `IDeepCloneableEntity<T>` expresses cloning support.
2. Group related service registrations in an `IDependencyInjectionModule`. Call `services.UseModule<TModule>(configuration, environment)` for a parameterless module, or pass an instance to `UseModule` when it needs options.
3. Keep the module's configuration choices in the application: pass `IConfiguration`, the host environment and any options factories from startup rather than hard-coding environment-specific values in domain code.

For a complete registration example using a concrete adapter, see the [module guide](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html#dependency-injection-modules).

See the [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/) and [dependency-injection guide](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html#dependency-injection-modules).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
