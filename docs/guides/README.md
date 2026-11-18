# Guides

Guides help you understand the platform and get work done with it. They teach; the [Conventions](../conventions/README.md) recommend, and the [Values and Decisions](../decisions/README.md) explain why. Contributors to the toolkit itself have their own [rules](../contributing/README.md).

![Architectural blueprint lines on a dark background with red-orange highlights, wide banner](../assets/images/headers/guides.png)

Not sure where to start? The [adoption advisor](../getting-started/advisor.md) asks a few questions about your project and suggests packages, steps and pages, in order.

## Onboarding: read in this order

| Guide | Answers |
|---|---|
| 1. [How the Platform Fits Together](mental-model.md) | What are the pieces, what is mine, how does a request or a job travel through them? |
| 2a. [Your First Solution](greenfield-solution.md) | How do I start a new solution, choose a layout and get to a green pull-request gate? |
| 2b. [Adopting an Existing Solution](adopting-existing-solution.md) | How do I move a running service onto the platform in small, deployable steps? |
| 3. [Bringing Your Company Stack](bringing-your-company-stack.md) | Where do my logging backend, auth, private feeds and deployment plug in? |
| 4. [Migration Recipes](migration-recipes.md) | How do I move [xUnit v2](https://xunit.net/), a [MockServer](https://www.mock-server.com/) container, `Add*` extensions, `IOptions`, health checks and hosted services? |
| 5. [Pitfalls](pitfalls.md) | Why did this break, and what do others do about it? |

## Platform features

| Guide | What it covers |
|---|---|
| [Layered Architecture](architecture.md) | The layers, the toolkit's DI modules, the layering check and the dependency map. |
| [Observability](observability.md) | `Observer`, where telemetry goes at runtime, plugging in your organization's setup, testing telemetry. |
| [Database Providers](database-providers.md) | The provider-agnostic persistence layer, [PostgreSQL](https://www.postgresql.org/)/Oracle/MySQL specifics, adding a provider. |
| [Mock Server](mock-server.md) | Standing in for HTTP upstreams: expectations, your own host, assertions, recording, migrating from a MockServer container. |
| [Building UIs](building-uis.md) | `Web.Ui`: [Blazor](https://learn.microsoft.com/aspnet/core/blazor/) startup, cookie-session login, environment catalog, downstream API clients, components. |
| [Fallout Builds](fallout-builds.md) | The `Build.Fallout` components, test discovery and runners, artifacts, releases and CLI tool wrappers. |
| [CLI Tool](../packages/Build.Cli.Tool/README.md) | The `anarchy-toolkit` CLI: bootstrapping a solution and scaffolding APIs, jobs and UIs. |

The [Package Catalog](../packages/README.md) documents each package, including the `Testing.*` kits.
