# Conventions

How the toolkit recommends writing applications on the platform. **They are recommendations:** the packages, the build and the CLI work without them, and every page ends with *Opting out or adapting*, which explains how to skip or bend the convention and what you give up.

In the toolkit repository itself they are rules; see [Contributing](https://anarchy-io.github.io/anarchy-toolkit/contributing/).

![Architectural blueprint lines on a dark background with red-orange highlights, wide banner](https://anarchy-io.github.io/anarchy-toolkit/assets/images/headers/guides.png)

## Why follow them at all?

Each convention exists for a reason recorded in the [values and decisions](../decisions/README.md). If the reason applies to your project, the convention saves you a decision; if it doesn't, opt out. A common path is to adopt the packages first and the conventions later, one at a time, as the [adoption walkthrough](https://anarchy-io.github.io/anarchy-toolkit/guides/adopting-existing-solution.html) does.

## Structure

| Convention | In short | Opt out by |
|---|---|---|
| [Layering](layering.md) | Projects in ordered layers, one folder per responsibility, checked by the build. | Not using `ILayeringBuild`, or folder mode and your own layer names. |
| [Code Organization](code-organization.md) | Shared layers, responsibility-based names, UseCases, aggregates and helpers. | Use the structure that fits your solution and record the choice. |
| [Dependency Injection](dependency-injection.md) | One module per concern, options supplied by the application, settings validated on start. | Calling the `Add*` extensions directly; keeping [`IOptions<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options.ioptions-1). |
| [Endpoints](endpoints.md) | Input/Output DTOs at the Web boundary, [`ActionResult<T>`](https://learn.microsoft.com/aspnet/core/web-api/action-return-types), contract-first interfaces. | Returning domain types, minimal APIs. |

## Code

| Convention | In short | Opt out by |
|---|---|---|
| [Async](async.md) | `Async` suffix, optional cancellation tokens, propagated everywhere. | Your own naming; tokens are still honoured if passed. |
| [Exceptions](exceptions.md) | Specialized exception types extending the `Core` bases. | Native exceptions and your own exception middleware. |
| [Enums](enums.md) | Enums for local vocabularies only, auxiliary tables for external values. | Enums with an `Unknown` fallback. |
| [Observability](observability.md) | Logs and metrics through `Observer` call sites, low-cardinality tags. | [`ILogger<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger-1) in your code, a custom `ObserverFactory`. |

## Tests

| Convention | In short | Opt out by |
|---|---|---|
| [Testing](testing.md) | `Unit`, `Integration` and `EndToEnd` projects, naming that mirrors `src/`, the `Testing.*` kits. | `TestKindAliases`, `IsTestProjectOfKind`, another framework. |
