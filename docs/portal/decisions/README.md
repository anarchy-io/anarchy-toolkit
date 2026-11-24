# Values and Decisions

Why the platform looks the way it does. The [conventions](../conventions/README.md) link here for their reasons: if a reason doesn't apply to your project, the convention probably doesn't either, and each convention explains how to opt out.

## Values

| Value | What it means in practice |
|---|---|
| **Adoptable piece by piece** | Each pillar, package, build component and convention can be adopted on its own, in an existing repository, in small deployable steps. Nothing requires a rewrite. |
| **Convention with an escape hatch** | Defaults are opinionated so a new solution needs no decisions, and every default can be overridden: build parameters, overridable members, plain [`IServiceCollection`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection) entry points. |
| **Pluggable** | Packages with concrete services register them through an `IDependencyInjectionModule` or an `IServiceCollection` extension. Modules never assume a configuration section: options always come from the application. |
| **Domain-free** | Generic code takes strings and generic parameters instead of domain enums. Repositories, clients, event payloads, message destinations and UI environments stay in your applications. |
| **Vendor-neutral** | Company specifics plug in through extension points (DI-registered logging providers and metric recorders, options, configuration hooks) instead of being built in. |
| **Standalone** | Every dependency restores from nuget.org; test-only libraries are confined to the `Testing.*` kits. |
| **Predictable at runtime** | Settings are validated on start, failures have specific types, cancellation is honoured, and telemetry describes every call site the same way. |
| **Explicit failures** | A failure is identifiable by its type and message, and still catchable by the native .NET type callers already handle. |
| **Automated, not documented** | A rule that matters is checked by the build (layering, lint, warnings as errors, skills validation) rather than by review. |

## Decisions

Architectural decision records, written after the fact for choices made while the toolkit grew. Each one records the context, the decision, its consequences and the alternatives considered.

| ADR | Decision |
|---|---|
| [0001](0001-layered-architecture.md) | Layered architecture, checked by the build |
| [0002](0002-dependency-injection-modules.md) | Dependency-injection modules that receive their configuration |
| [0003](0003-validated-app-settings.md) | One validated settings object per application |
| [0004](0004-telemetry-composition.md) | Telemetry composed over the standard .NET logging and metrics APIs |
| [0005](0005-xunit-v3-on-mtp.md) | [xUnit v3](https://xunit.net/) on [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro) |
| [0006](0006-fallout-build.md) | [Fallout](https://fallout.build/) for the build, without deployment components |
| [0007](0007-own-mock-server.md) | An in-process mock server instead of a [MockServer](https://www.mock-server.com/) container |
| [0008](0008-test-levels.md) | Three test levels, one project per level |
| [0009](0009-optional-automation-preferences.md) | Optional automation preferences, precedence and stable public defaults |
| [0010](0010-kafka-messaging-transport.md) | Kafka as a second messaging transport |
| [0011](0011-events-persistence-package.md) | Event and outbox persistence in a separate package |
