# Guides

Conventions for working on the toolkit and on applications built with it. Each guide stands on its own; start with **Layered Architecture**, which explains where code goes.

![Architectural blueprint lines on a dark background with red-orange highlights, wide banner](../assets/images/headers/guides.png)

## Foundations

| Guide | What it covers |
|---|---|
| [Layered Architecture](architecture.md) | `Core < Infrastructure < Configuration < Hosting < Web < Build`, project structure, DI modules, the layering check and the dependency map. |
| [Fallout Builds](fallout-builds.md) | The `Build.Fallout` components, test discovery and runners, artifacts, releases and generated CLI tool wrappers. |

## Writing code

| Guide | What it covers |
|---|---|
| [Async](async-conventions.md) | The `Async` suffix, optional cancellation tokens and how to propagate them, worker shutdowns. |
| [Enums](enum-conventions.md) | When to use enums, `[Description]` mapping, auxiliary tables and `AttributeIdCacheBase`. |
| [Exceptions](exception-conventions.md) | Specialized exceptions, the `Core` bases, mid-level exceptions and where exception types live. |
| [Observability](observability.md) | `Observer`, call sites, log and metric conventions, runtime sinks per host, plugging in your organization's setup. |

## Building applications

| Guide | What it covers |
|---|---|
| [Exposing Endpoints](exposing-endpoints.md) | Presentation contracts, Input/Output DTOs with `ToDto`/`FromDto`, `ActionResult<T>`. |
| [Building UIs](building-uis.md) | `Web.Ui`: Blazor startup, cookie-session login, environment catalog, downstream API clients, reusable components. |
| [Database Providers](database-providers.md) | The provider-agnostic persistence layer, PostgreSQL/Oracle/MySQL specifics, adding a provider. |

## Quality

| Guide | What it covers |
|---|---|
| [Testing](testing-conventions.md) | Test project layout and naming, test levels, the `Testing.*` kits, fixtures and mocks. |
