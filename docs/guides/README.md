# Guides

Conventions for working on the toolkit and on applications built with it.

| Guide | What it covers |
|---|---|
| [Layered Architecture](architecture.md) | `Core < Infrastructure < Configuration < Hosting < Web < Build` by project-name prefix, DI modules, the layering check. |
| [Async Conventions](async-conventions.md) | `Async` suffix, optional cancellation tokens and how to propagate them, worker shutdowns. |
| [Enum Conventions](enum-conventions.md) | When to use enums, `[Description]` mapping, auxiliary tables and `AttributeIdCacheBase`. |
| [Exception Conventions](exception-conventions.md) | Specialized exceptions, the `Core` bases, mid-level exceptions, `Exceptions` folder placement. |
| [Exposing Endpoints](exposing-endpoints.md) | Presentation contracts, Input/Output DTOs with `ToDto`/`FromDto`, `ActionResult<T>`. |
| [Observability](observability.md) | `Observer`, call sites, log and metric conventions, runtime sinks per host. |
| [Building UIs](building-uis.md) | `Web.Ui`: Blazor startup, cookie-session login, environment catalog, downstream API clients, reusable components. |
| [Testing Conventions](testing-conventions.md) | Test project layout and naming, test levels, the `src/Testing` kits, fixtures and mocks. |
| [Fallout Builds](fallout-builds.md) | `Build.Fallout` build components, test discovery and runners, artifacts, generated CLI tool wrappers. |
