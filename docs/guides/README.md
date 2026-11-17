# Guides

Guides for the platform's three pillars (Architecture, Infrastructure and Build Ecosystem) and the engineering practices shared by the toolkit and the applications built with it. The conventions they describe are **rules within the toolkit itself** and **recommendations for applications built on it**: adopters can follow, adapt or ignore them. Each guide stands on its own; start with **Layered Architecture**, which explains where code goes.

![Architectural blueprint lines on a dark background with red-orange highlights, wide banner](../assets/images/headers/guides.png)

## Architecture

| Guide | What it covers |
|---|---|
| [Layered Architecture](architecture.md) | `Core < Infrastructure < Configuration < Hosting < Web < Build`, project structure, DI modules, the layering check and the dependency map. |

## Infrastructure

| Guide | What it covers |
|---|---|
| [Observability](observability.md) | `Observer`, call sites, log and metric conventions, runtime sinks per host, plugging in your organization's setup. |
| [Database Providers](database-providers.md) | The provider-agnostic persistence layer, PostgreSQL/Oracle/MySQL specifics, adding a provider. |
| [Exposing Endpoints](exposing-endpoints.md) | Presentation contracts, Input/Output DTOs with `ToDto`/`FromDto`, `ActionResult<T>`. |
| [Building UIs](building-uis.md) | `Web.Ui`: Blazor startup, cookie-session login, environment catalog, downstream API clients, reusable components. |

The [Package Catalog](../packages/README.md) documents each infrastructure package, including the `Testing.*` kits.

## Build Ecosystem

| Guide | What it covers |
|---|---|
| [Fallout Builds](fallout-builds.md) | The `Build.Fallout` components, test discovery and runners, artifacts, releases and generated CLI tool wrappers. |
| [CLI Tool](../packages/Build.Cli.Tool/README.md) | The `anarchy-toolkit` CLI: bootstrapping a solution and scaffolding APIs, jobs and UIs. |

## Engineering Practices

| Guide | What it covers |
|---|---|
| [Async](async-conventions.md) | The `Async` suffix, optional cancellation tokens and how to propagate them, worker shutdowns. |
| [Enums](enum-conventions.md) | When to use enums, `[Description]` mapping, auxiliary tables and `AttributeIdCacheBase`. |
| [Exceptions](exception-conventions.md) | Specialized exceptions, the `Core` bases, mid-level exceptions and where exception types live. |
| [Testing](testing-conventions.md) | Test project layout and naming, test levels, the `Testing.*` kits, fixtures and mocks. |
