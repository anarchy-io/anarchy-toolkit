# Anarchy.Toolkit.Core

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

For a complete registration example using a concrete adapter, see the [module guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/architecture.md#dependency-injection-modules).

See the [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme) and [dependency-injection guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/architecture.md#dependency-injection-modules).

## Releases

### 1.0.5

- refactor(src): clean up code style across library packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced entity contracts, shared exceptions and dependency-injection module composition.
