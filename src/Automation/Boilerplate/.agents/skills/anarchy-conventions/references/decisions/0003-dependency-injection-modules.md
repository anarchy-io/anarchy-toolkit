# 0003. Dependency-injection modules that receive their configuration

## Context

Libraries that read `configuration.GetSection("Postgres")` themselves dictate the application's settings layout, and `Startup` classes with hundreds of `services.Add...` lines are hard to test or reuse.

## Decision

A package with concrete services exposes `IDependencyInjectionModule`s (`Core/Composition`), applied with `services.UseModule(module, configuration, environment)`. A module takes its options as constructor parameters, usually `Func<IConfiguration, TOptions>`, and wraps any existing `Add*` extension instead of replacing it.

## Consequences

- The application decides section names, settings types and secret sources in its composition root.
- A module can be registered in isolation in a test and its graph resolved.
- Applications not using modules call the same `Add*` extensions.
- One more type per concern, compared with a plain extension method.

## Alternatives considered

- **Extension methods only.** Simpler, but they tend to read configuration themselves and can't be passed around as values.
- **A third-party module system ([Autofac](https://autofac.org/) modules).** Ties every consumer to one container.
