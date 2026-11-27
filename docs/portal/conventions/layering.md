# Layering

How code is split into layers, projects and folders, so that every file has one obvious place and dependencies only point down: `Core < Infrastructure < Bootstrap < Runtime < Apps` in a solution, `Core < Infrastructure < Bootstrap < Runtime < Automation` in the toolkit itself. This layered style, with the domain at the centre and every dependency pointing inward, is sometimes called an [onion architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/). The [Layered Architecture guide](../guides/architecture.md) explains the layers themselves and the tooling around them.

The first chain is the default `layers` for a solution: `Apps` holds the APIs, jobs and UIs and has no library project of its own. The toolkit repository replaces `Apps` with `Automation` (build tooling), on top so that no shipped library can depend on it; a solution gets no Automation library, its build lives in `build/_build.csproj`. A solution may list its own layer names. Optional overrides live in the root `.anarchy-toolkit.json`; see [Automation Preferences](../guides/automation-preferences.md).

> **Recommendation, not a requirement.** The packages work without following this convention. It is a rule only for code in the toolkit repository (see [Contributing](../contributing/README.md)); applications can adopt it, adapt it or skip it. [Opting out or adapting](#opting-out-or-adapting) explains how.
{: .notice--info}

## Why

- **Dependencies you can see.** The business core never depends on a database driver, a host or a web framework, so it can be tested and reused without them. See [ADR 0001](../decisions/0001-layered-architecture.md).
- **One place per file.** Layer, then responsibility, then namespace: no team debate per pull request.
- **Checked, not reviewed.** `make check_layering` turns the rule into a failing build instead of a review comment.

## Choose one project-layering mode

For adopters, the recommended layout determines a project's layer from its folder: for example,
`src/Library/Core/Core.csproj` and `src/Library/Infrastructure/Infrastructure.csproj`, with applications under
`src/Apps/Apis/<Name>`, `src/Apps/Jobs/<Name>` and `src/Apps/Ui/<Name>`. A single-application solution
can put its layer projects directly under `src/`. See [Code Organization](code-organization.md) for the layouts and
the trade-offs of shared library dependencies.

The alternative is project-name prefixes, such as `Core.Orders` and `Infrastructure.Orders.Postgres` (bootstrapped as
`src/Library.<Layer>` and `src/Apps.Api.<Name>`, `src/Apps.Job.<Name>`, `src/Apps.Ui.<Name>`). The toolkit
itself uses this mode, which is useful for separately distributed packages but available to any solution.

Choose one mode for the whole solution. Folder mode determines layers from configured paths; prefix mode determines
them from project names. Neither mode falls back to the other. Mixing detection modes is unsupported; bypassing the
checks to mix them is strongly discouraged. Layer-named folders inside a project organize its code and are separate
from this choice.

The solution can save its choice in the optional root `.anarchy-toolkit.json` ([schema](../schemas/anarchy-toolkit.schema.json)); bootstrap and scaffold can also receive layout arguments directly. The scaffolding, `make check_layering` and `make dependency_map` resolve explicit arguments, saved overrides, then in-code defaults. A missing file or mode is valid: the baseline is folder mode (`src/Library`, standard application folders and layers). The tooling never guesses a mode from project names or paths. Invalid JSON, an unknown mode, contradictory folder settings in prefix mode, and invalid effective mappings are reported:

```json
{
  "$schema": "https://anarchy-io.github.io/anarchy-toolkit/schemas/anarchy-toolkit.schema.json",
  "layering": { "mode": "folder", "libraryRoot": "src/Library" }
}
```

`layering` is only one optional section in the shared file. Its optional `mode` is `folder` or `prefix`; the default
is `folder`. `layers` (innermost first) defaults to the chain above; `root` defaults to `src`. Folder mode defaults
`libraryRoot` to `src/Library` and maps the standard application folders (`src/Apps/Apis`, `src/Apps/Jobs`, `src/Apps/Ui`) to
the `Apps` layer. A single-application layout can explicitly set `libraryRoot` to `src` and map its app folder. Prefix mode
suppresses folder-mode fallbacks and uses project names. All properties may be omitted; consult the
[schema](../schemas/anarchy-toolkit.schema.json) for the complete preference file.

## Inside a project

A project's own code sits **directly at its root** and is organized by responsibility folders. Use plural folder and namespace names such as `Repositories/`, `UseCases/`, `Services/Aggregates/`, and `Extensions/`; see [Code Organization](code-organization.md#inside-a-project) and [Naming](naming.md#namespaces) for the conventions.

```text
src/Core.Orders/
├── Events/
├── Helpers/
├── UseCases/
├── Services/Aggregates/
└── Extensions/
```

A root folder named after a layer (`Core/`, `Infrastructure/`, `Bootstrap/`, `Runtime/`) is only allowed for a layer **below** the project's own. It holds the classes the project introduces for that inner layer:

| Project layer | Allowed layer folders | Example |
|---|---|---|
| `Core.*` | none: a Core project never has a `Core/` folder | `Core.Health/Checks/...` |
| `Infrastructure.*` | `Core/` | `Infrastructure.Orders/Core/Repositories/Orders/IOrderRepository.cs`, but never `Infrastructure/`, `Bootstrap/` or `Runtime/` |
| `Bootstrap.*` | `Core/`, `Infrastructure/` | `Bootstrap.Aws/Infrastructure/...`, `Bootstrap.Orders/Infrastructure/Repositories/Orders/Postgres/...` |
| `Runtime.*` | `Core/`, `Infrastructure/`, `Bootstrap/` | `Runtime.Web.Ui/Infrastructure/Http/...` |
| `Automation.*` | any lower layer | `Automation.Build/Core/Entity/Build/...` |

So `Infrastructure.Persistence.Db.Oracle` may get a `Core/` folder if it has to expose its own domain entities, but code of its own layer, including its DI modules, goes at the root, never under `Infrastructure/` or `Bootstrap/`. Layer folders do not change the project's layer either: a `Core/` folder inside an `Infrastructure.*` project is still Infrastructure code, so it may only use projects up to Infrastructure.

## Where does a new file go?

1. **Pick the project by layer, then the folder by responsibility.** Code of the project's own layer goes at its root. Only code for an inner layer gets a layer folder: a repository contract is `Core/Service/Repository/...` even when it ships in an Infrastructure package (see `Infrastructure.Persistence.Db.Events/Core/Service/Repository/Storage/Event/`).
2. **Extension classes** live under the project's root `Extensions/` folder, mirroring the extended type's namespace
   (e.g. `Extensions/Microsoft/Extensions/DependencyInjection/`); see [Code Organization](code-organization.md#extensions).
3. **Embedded SQL** sits in a `Queries/` folder next to the repository that loads it, because resources are resolved from the repository's namespace (`<Namespace>.Queries.<Name>.sql`).
4. **A new project** must follow the solution's selected layering mode: its name needs a known layer prefix in prefix
   mode; in folder mode its path must map to a layer or a configured application folder.
5. **`Composition/` vs `Configuration/`.** An application's composition root (its startup, entry point and DI wiring, e.g. `Runtime.Web/Composition/WebAppStartupBase.cs` or a sample's `Composition/SampleApiStartup.cs`) goes under `Composition/`. `Configuration/` is only for settings and options (e.g. `Entity/Configuration/.../PostgresConnectionOptions.cs`).

## Namespace collisions to know about

The responsibility folders produce a few namespace segments that collide with type names:

- **`Attribute`.** Under `.../Entity/Attribute/` or `Presentation/Attribute/`, derive from [`System.Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute) explicitly.
- **`Host`.** A `Host/` folder (e.g. `Runtime/Host/`) can shadow [`Microsoft.Extensions.Hosting.Host`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.hosting.host) for code in neighboring namespaces. If you get `CS0118` on `Host.CreateDefaultBuilder(...)`, use the full type name.

For toolkit-owned types, prevent [`CS0118`](https://learn.microsoft.com/dotnet/csharp/misc/cs0118) ("is a namespace but is
used like a type") and `CS0104` (ambiguous reference) with a descriptive name or meaningful suffix, as the
[naming convention](naming.md#type-suffixes) requires. Automation's `ConfigurationValueSet`, `EnvironmentValueSet` and
`RuntimeValueSet` make their role clear without colliding with architectural namespaces or .NET types. Reserve
qualification or an alias for unavoidable external names rather than using aliases to compensate for toolkit names.

## Opting out or adapting

| You want to... | Do this |
|---|---|
| Keep your current project layout | Leave `ILayeringBuild` out of `build/Build.cs`. Nothing else in the toolkit reads your layout. |
| Keep layers, but organize by folder (`src/Library/Core/...`) | Use the folder baseline, or set `layering.libraryRoot` in `.anarchy-toolkit.json`; set `layering.root` to check only one subtree. |
| Use other layer names or more layers | Set `layering.layers` in `.anarchy-toolkit.json`. |
| Map other application folders to a layer | Set `layering.applicationFolders` in `.anarchy-toolkit.json` (e.g. `{ "src/Services": "Apps", "src/Workers": "Apps" }`), as in the [greenfield walkthrough](../guides/greenfield-solution.md#choose-a-layout). |
| Adopt it gradually | Run the check locally first, fix one project at a time, then add `check_layering` to the pull-request gate. |

**What you lose by opting out:** the layering check and the red edges in the [dependency map](../guides/architecture.md#dependency-map). The scaffolded APIs, jobs and UIs (`anarchy-toolkit scaffold`) assume the layered layout and need moving files if you use another one.
