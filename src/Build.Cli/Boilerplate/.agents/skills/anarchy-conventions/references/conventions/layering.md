# Layering

How code is split into layers, projects and folders, so that every file has one obvious place and dependencies only point down: `Core < Infrastructure < Configuration < Hosting < Web < Build`. The [Layered Architecture guide](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html) explains the layers themselves and the tooling around them.

> **Recommendation, not a requirement.** The packages work without following this convention. It is a rule only for code in the toolkit repository (see [Contributing](https://anarchy-io.github.io/anarchy-toolkit/contributing/)); applications can adopt it, adapt it or skip it. [Opting out or adapting](#opting-out-or-adapting) explains how.

## Why

- **Dependencies you can see.** The business core never depends on a database driver, a host or a web framework, so it can be tested and reused without them. See [ADR 0001](../decisions/0001-layered-architecture.md).
- **One place per file.** Layer, then responsibility, then namespace: no team debate per pull request.
- **Checked, not reviewed.** `make check_layering` turns the rule into a failing build instead of a review comment.

## Choose one project-layering mode

For adopters, the recommended layout determines a project's layer from its folder: for example,
`src/Library/Core/Core.csproj` and `src/Library/Infrastructure/Infrastructure.csproj`. A single-application solution
can put its layer projects directly under `src/`. See [Code Organization](code-organization.md) for the layouts and
the trade-offs of shared library dependencies.

The alternative is project-name prefixes, such as `Core.Orders` and `Infrastructure.Orders.Postgres`. The toolkit
itself uses this mode, which is useful for separately distributed packages but available to any solution.

Choose one mode for the whole solution. Folder mode determines layers from configured paths; prefix mode determines
them from project names. Neither mode falls back to the other. Mixing detection modes is unsupported; bypassing the
checks to mix them is strongly discouraged. Layer-named folders inside a project organize its code and are separate
from this choice. `LayeringApplicationFolders` maps application paths to layers when using folder mode.

## Inside a project

A project's own code sits **directly at its root**, organized by responsibility (`Entity/`, `Service/`, `Repository/`, `Extension/`, `DependencyInjection/`, ...), and namespaces follow folders:

```text
src/Infrastructure.Persistence.Postgres/
├── DependencyInjection/Module/ModulePostgres.cs
├── Entity/Configuration/Storage/Database/PostgresConnectionOptions.cs
├── Repository/Storage/Database/Postgres/PostgresDbReaderRepositoryBase.cs
└── Service/Storage/Database/Postgres/PostgresDbUnitOfWork.cs
```

```csharp
namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;
```

A root folder named after a layer (`Core/`, `Infrastructure/`, `Configuration/`, `Web/`) is only allowed for a layer **below** the project's own. It holds the classes the project introduces for that inner layer:

| Project layer | Allowed layer folders | Example |
|---|---|---|
| `Core.*` | none: a Core project never has a `Core/` folder | `Core.Health/Service/...` |
| `Infrastructure.*` | `Core/` | `Infrastructure.Persistence.Db/Core/Service/Repository/...` (repository contracts it exposes), but never `Infrastructure/`, `Configuration/` or `Web/` |
| `Configuration.*` | `Core/`, `Infrastructure/` | `Configuration/Infrastructure/Entity/...` |
| `Hosting.*` | `Core/`, `Infrastructure/`, `Configuration/` | none yet |
| `Web.*` | `Core/`, `Infrastructure/`, `Configuration/`, `Hosting/` | `Web.Ui/Core/Service/Ui/...` |
| `Build.*` | any lower layer | `Build.Fallout/Core/Entity/Build/...` |

So `Infrastructure.Persistence.Oracle` may get a `Core/` folder if it has to expose its own domain entities, but code of its own layer, including its DI modules, goes at the root, never under `Infrastructure/` or `Configuration/`. Layer folders do not change the project's layer either: a `Core/` folder inside an `Infrastructure.*` project is still Infrastructure code, so it may only use projects up to Infrastructure.

## Where does a new file go?

1. **Pick the project by layer, then the folder by responsibility.** Code of the project's own layer goes at its root. Only code for an inner layer gets a layer folder: a repository contract is `Core/Service/Repository/...` even when it ships in an Infrastructure package (see `Infrastructure.Persistence.Db/Core/Service/Repository/Storage/Event/`).
2. **Extension classes** live under `Extensions/`, organized by subject, and use the subject's namespace. Existing
   toolkit extensions under `Extension/` and their published namespaces are legacy APIs being migrated selectively;
   see [Code Organization](code-organization.md#names-and-responsibilities).
3. **Embedded SQL** sits in a `Queries/` folder next to the repository that loads it, because resources are resolved from the repository's namespace (`<Namespace>.Queries.<Name>.sql`).
4. **A new project** must follow the solution's selected layering mode: its name needs a known layer prefix in prefix
   mode; in folder mode its path must map to a layer or a configured application folder.
5. **`Composition/` vs `Configuration/`.** An application's composition root (its startup, entry point and DI wiring, e.g. `Web/Composition/WebAppStartupBase.cs` or a sample's `Composition/Startup.cs`) goes under `Composition/`. `Configuration/` is only for settings and options (e.g. `Entity/Configuration/.../PostgresConnectionOptions.cs`).

## Namespace collisions to know about

The responsibility folders produce a few namespace segments that collide with type names:

- **`Event`.** `Core.Events` has both an `Event` entity and `.../Event/...` folders. In files under a namespace that has an `Event` child namespace, add an alias right after the file-scoped namespace line (usings placed there are resolved before parent namespaces):

  ```csharp
  namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain;

  using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;
  ```

- **`Attribute`.** Under `.../Entity/Attribute/` or `Presentation/Attribute/`, derive from [`System.Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute) explicitly.
- **`Host`.** A `Host/` folder (e.g. `Hosting/Host/`) can shadow [`Microsoft.Extensions.Hosting.Host`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.host) for code in neighboring namespaces. If you get `CS0118` on `Host.CreateDefaultBuilder(...)`, use the full type name.

The general fix for [`CS0118`](https://learn.microsoft.com/dotnet/csharp/misc/cs0118) ("is a namespace but is used like a type") or `CS0104` (ambiguous reference) is the same: an alias placed right after the file-scoped namespace line.
## Opting out or adapting

| You want to... | Do this |
|---|---|
| Keep your current project layout | Leave `ILayeringBuild` out of `build/Build.cs`. Nothing else in the toolkit reads your layout. |
| Keep layers, but organize by folder (`src/Library/Core/...`) | `make check_layering BY_FOLDER=true`, and `LAYERING_ROOT` to check only one subtree (e.g. `src/Library`). |
| Use other layer names or more layers | Override `LayeringLayers` and `GetLayer` in `build/Build.cs`. |
| Map whole applications to a layer (APIs are Web, jobs are Hosting) | Folder mode plus `LayeringApplicationFolders` in `.fallout/parameters.json` (e.g. `["src/Api=Web", "src/Jobs=Hosting"]`), as in the [greenfield walkthrough](https://anarchy-io.github.io/anarchy-toolkit/guides/greenfield-solution.html#choose-a-layout). |
| Adopt it gradually | Run the check locally first, fix one project at a time, then add `check_layering` to the pull-request gate. |

**What you lose by opting out:** the layering check and the red edges in the [dependency map](https://anarchy-io.github.io/anarchy-toolkit/guides/architecture.html#dependency-map). The scaffolded APIs, jobs and UIs (`anarchy-toolkit scaffold`) assume the layered layout and need moving files if you use another one.
