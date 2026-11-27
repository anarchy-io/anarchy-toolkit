# Code Organization

Organize projects around shared layers, deployment, distribution, or other meaningful compilation boundaries. Inside a project, group code by responsibility rather than creating a project for every class or feature. [Naming](naming.md) covers type, namespace and suffix names; [UseCases and Aggregates](usecases-and-aggregates.md) covers the workflow and coordination types.

![Architectural blueprint lines on a dark background with red-orange highlights, wide banner](../assets/images/headers/guides.png)

> **Recommendation, not a requirement.** Adopt these conventions where they help your solution; they describe where new code goes. Each package's README shows the organization of its public API.
{: .notice--info}

## Why

- **Find the code.** Responsibility-oriented names make the purpose of a class clear without relying on a generic `Service` or `Entity` namespace. See [layered architecture](../decisions/0001-layered-architecture.md).
- **Share deliberately.** A monorepo's applications can build on a common library without turning each application feature into a separately distributed package.
- **Keep orchestration visible.** UseCases describe workflows; aggregates coordinate resources when consistency or persistence requires it. Neither replaces a repository's responsibility for queries and individual writes.

## Projects versus folders

A **project root** is a folder containing a `.csproj`. Create a project when one of these holds:

- independent NuGet distribution;
- independent deployment;
- independently versioned contracts;
- meaningful compilation or dependency isolation;
- a separate test suite;
- a shared architectural layer reused by several applications.

Everything else is a **folder**. A folder inside a project root, even one named after a layer (`Core/`, `Infrastructure/`, `Bootstrap/`, `Runtime/`), is ordinary source organization: it does not make a second project and does not change the project's own layer or relax the dependency direction. An application uses such folders to extend the shared libraries without a project per layer.

This is separate from the [layering mode](layering.md#choose-one-project-layering-mode) (how the build finds a project's layer, by folder or by name prefix). Both modes enforce the same dependency rules, and neither implies that projects are NuGet packages. Choose one mode for the whole solution.

## Recommended layouts

These are strong recommendations, not requirements. Include only the applications and supporting folders your solution needs.

### Monorepo

Shared layer projects live under `src/Library/`; deployable applications are separate projects, grouped by kind:

```text
.
├── .anarchy-toolkit.json         # optional automation preference overrides
├── build/                        # _build.csproj, the Anarchy build
├── database/                     # database resources and migrations, when needed
├── docker/                       # container configuration, when needed
├── docs/
├── scripts/
├── Makefile
├── Directory.Packages.props
├── src/
│   ├── Library/
│   │   ├── Core/                 # shared Core.csproj
│   │   ├── Infrastructure/       # shared Infrastructure.csproj
│   │   ├── Bootstrap/            # shared Bootstrap.csproj
│   │   └── Runtime/              # shared Runtime.csproj
│   └── Apps/
│       ├── Apis/
│       │   └── Backoffice/       # application project
│       ├── Jobs/
│       │   └── Processor/        # application project
│       └── Ui/
│           └── AdminPanel/       # application project
└── tests/
    ├── Common/                   # shared test resources
    ├── Unit/
    ├── Integration/
    ├── EndToEnd/
    └── MockServer/               # optional HTTP mock server
```

The shared reference direction is `Apps → Runtime → Bootstrap → Infrastructure → Core`: an arrow means "references". The `Apps` layer has no library project: APIs, jobs and UIs reference the shared Runtime library. Applications can share settings (`scaffolding.appSettings: shared` in `.anarchy-toolkit.json`): the single `AppSettings` then lives in Infrastructure, and the `ModuleAppSettings` with the `appsettings*.json` files in Bootstrap; by default (`isolated`) each application has its own.

Each application project can hold its own `Core/`, `Infrastructure/`, `Bootstrap/`, `Runtime/`, `Composition/` or `Presentation/` folders to extend the shared libraries, still pointing toward lower layers. In folder mode, the default mapping assigns `src/Apps/Apis`, `src/Apps/Jobs` and `src/Apps/Ui` to the `Apps` layer; override them under `layering.applicationFolders` in `.anarchy-toolkit.json` ([Layering](layering.md#choose-one-project-layering-mode)).

### Single application

For one application, put the layer projects directly under `src/`, without a `Library` grouping:

```text
src/
├── Core/                         # Core.csproj
├── Infrastructure/               # Infrastructure.csproj
├── Bootstrap/                    # Bootstrap.csproj
├── Runtime/                      # Runtime.csproj
└── App/                          # the application project
```

Dependencies point down: `App → Runtime → Bootstrap → Infrastructure → Core`. Name the application project `App`, `Api`, `Job`, `Program`, `Cli` or after the solution. For this layout, set `layering.libraryRoot` to `src` and map the application folder in `.anarchy-toolkit.json`, or pass those choices directly to the operation.

### Tests

Keep unit, integration and end-to-end testing as distinct suites, even if the project names differ. A shared `tests/Common/` project holds fixtures, mocks and helpers, including extensions of the `Testing.*` packages. Unit and integration file locations and namespaces mirror their system under test; end-to-end suites cover larger application flows. Each test project references the applications, libraries and common resources it exercises. See [Testing](testing.md).

## Shared library dependencies and their trade-offs

Prefer adding Anarchy packages and other third-party dependencies to the appropriate shared library layer, even when that project only exposes their DI modules or wraps them in solution-specific abstractions. Applications then share one foundation, and application-specific features stay in the applications.

The cost is that an application can carry assemblies it never uses, through transitive references, and ship larger artifacts (a Lambda package, a container image). The choice is yours: weigh reuse, isolation and deployment size per dependency. A dependency used by a single application belongs in that application; centralizing is not mandatory.

## Inside a project

Within a project root, use folders for responsibilities rather than separate projects, and include only the ones the project needs:

```text
Core/
├── Repositories/               # contracts, grouped by resource
├── Events/
├── Helpers/                    # pure static helpers, grouped by subject
├── UseCases/                   # business workflows
├── Services/
│   └── Aggregates/             # aggregates are services
└── Extensions/                 # extension classes, mirroring their subject
```

Namespaces follow folders and use plural segments (see [Naming](naming.md#namespaces)). When the project boundary already names a layer (a prefix-layered `Core.Orders` project), do not repeat the layer inside its namespaces or folders without a reason.

### Helpers

General-purpose static helpers go in a project-root `Helpers/` folder, grouped by subject (`Helpers/Dates/DateHelper.cs`, namespace `…Core.Helpers.Dates`). They are deterministic where practical and do not depend on DI, application state or infrastructure: date and time manipulation, collection and string handling, pure conversions, stateless formatting. Technology-specific and test helpers may use whatever their layer allows. A helper is not a substitute for a service or domain abstraction, and `Helpers` is not a dumping ground.

### Extensions

Extension classes go in a project-root `Extensions/` folder whose subfolders mirror **the extended type's namespace**. The class takes the `Extensions` suffix, is static, and its namespace follows its folder:

```text
Extensions/Microsoft/Extensions/DependencyInjection/ServiceCollectionExtensions.cs  // namespace …Core.Extensions.Microsoft.Extensions.DependencyInjection
Extensions/System/StringExtensions.cs                                               // namespace …Core.Extensions.System
```

See [ADR 0002](../decisions/0002-extensions-and-helpers.md) for why namespaces follow folders for extensions too.

Group extensions by subject and responsibility instead of collecting them in a generic class.

### Resource-based adapters

When a technology-specific implementation fulfills a Core contract, group by **resource** first and **technology** second:

```text
src/
├── Core/
│   └── Repositories/
│       └── Orders/
│           └── IOrderRepository.cs
└── Infrastructure/
    └── Repositories/
        └── Orders/
            ├── Postgres/
            │   └── PostgresOrderRepository.cs
            └── DynamoDb/
                └── DynamoDbOrderRepository.cs
```

```csharp
namespace Acme.Core.Repositories.Orders;                    // the contract
namespace Acme.Infrastructure.Repositories.Orders.Postgres; // one adapter
namespace Acme.Infrastructure.Repositories.Orders.DynamoDb; // another
```

1. Core owns the abstraction.
2. Infrastructure owns the technology-specific implementations.
3. Resource folders group related contracts and implementations; technology folders tell adapters apart.
4. The composition root selects and registers the implementation.
5. Core never depends on an infrastructure technology, and the choice of technology never leaks into the contract.

This holds in folder-mode layouts and in prefix-layered projects alike. The toolkit's database packages follow it at package level: contracts in `Core.Persistence.Abstractions`, the shared, business-neutral relational layer in `Infrastructure.Persistence.Db`, event-store bases and outbox contracts in `Infrastructure.Persistence.Db.Events`, one adapter per database in `Infrastructure.Persistence.Db.<Provider>` ([Database Providers](../guides/database-providers.md)).

## Summary

| Concern | Convention |
|---|---|
| Projects | Shared layers, distribution, deployment or meaningful compilation boundaries |
| Layering mode | One per solution: folder (recommended) or prefix; never mixed |
| Layout | Shared `Library` projects plus application projects (monorepo); layer projects directly under `src/` (single application) |
| Folders | Internal organization; application-local layer folders extend shared libraries |
| Tests | Separate unit, integration and end-to-end suites; unit and integration paths mirror the system under test |
| Namespaces, types, services | See [Naming](naming.md) |
| UseCases, Aggregates | See [UseCases and Aggregates](usecases-and-aggregates.md) |
| Helpers | Static, pure, subject-grouped, outside DI |
| Extensions | Root `Extensions/` folder mirroring the extended type's namespace; namespace follows the folder |
| Adapters | Resource folder, then technology folder |
| DI | The composition root selects implementations |

## Opting out or adapting

Use the structure and naming that suit the solution; record deliberate differences in its architecture decisions or `AGENTS.md`. A smaller service may not benefit from explicit UseCases or aggregates, and some dependencies belong in one application rather than a shared library. You give up some consistency between projects and must explain where new code belongs, but the toolkit packages do not require these folders or names.
