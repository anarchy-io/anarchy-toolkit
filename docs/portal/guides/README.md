# Guides

Guides help you understand the platform and get work done with it. They teach; the [Conventions](../conventions/README.md) recommend, and the [Values and Decisions](../decisions/README.md) explain why. Contributors to the toolkit itself have their own [rules](../contributing/README.md).

![Architectural blueprint lines on a dark background with red-orange highlights, wide banner](../assets/images/headers/guides.png)

Not sure where to start? The [adoption advisor](../getting-started/advisor.md) asks a few questions about your project and suggests packages, steps and pages, in order.

## Onboarding: read in this order

| Guide | Answers |
|---|---|
| 1. [How the Platform Fits Together](mental-model.md) | What are the pieces, what is mine, how does a request or a job travel through them? |
| 2a. [Your First Solution](greenfield-solution.md) | How do I start a new solution, choose a layout and get to a green pull-request gate? |
| 2b. [Adopting an Existing Solution](adopting-existing-solution.md) | How do I add the platform to a repository that already runs a service, in small, deployable steps? |
| 3. [Bringing Your Company Stack](bringing-your-company-stack.md) | Where do my logging and metrics backend, auth, private feeds and deployment plug in? |
| 4. [Onboarding Recipes](onboarding-recipes.md) | How do I set up the first test project, the mock server, a module, validated settings, a health check, a worker and telemetry? |
| 5. [Pitfalls](pitfalls.md) | Why did this break, and what do others do about it? |

## Platform features

| Guide | What it covers |
|---|---|
| [Layered Architecture](architecture.md) | The layers, the toolkit's DI modules, the layering check and the dependency map. |
| [Automation Preferences](automation-preferences.md) | Optional root-level automation overrides, settings inventory, defaults, precedence and explicit editing commands. |
| [Observability](observability.md) | `ICoreTelemetry` and `AppTelemetry`, standard logging and metrics, OTLP, optional Serilog, correlation IDs, Datadog and New Relic configuration, testing telemetry. |
| [Database Providers](database-providers.md) | The provider-agnostic persistence layer, [PostgreSQL](https://www.postgresql.org/)/Oracle/MySQL specifics, adding a provider. |
| [Mock Server](mock-server.md) | Standing in for HTTP upstreams: expectations, your own host, assertions and recording. |
| [Building UIs](building-uis.md) | `Web.Ui`: [Blazor](https://learn.microsoft.com/aspnet/core/blazor/) startup, cookie-session login, environment catalog, downstream API clients, components. |
| [Anarchy Automation Builds](automation-builds.md) | The `Automation.Build` components, test discovery and runners, artifacts, releases and CLI tool wrappers. |
| [GitHub Package Publishing](github-package-publishing.md) | Repository-owner setup for the toolkit's NuGet publishing workflow and `main` protections. |
| [CLI Tool](../packages/Automation.Build.Cli/README.md) | The `anarchy-toolkit` CLI: bootstrapping a solution and scaffolding APIs, jobs and UIs. |

The [Package Catalog](../packages/README.md) documents each package, including the `Testing.*` kits, and answers [Which package do I need?](../packages/README.md#which-package-do-i-need) for the families that are split by role (`Automation*`, `Infrastructure.Persistence.Db.*` and `Testing.Db.*`, `Core.Telemetry` and `Infrastructure.Telemetry.Serilog`).
