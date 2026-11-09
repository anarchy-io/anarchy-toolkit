# Layered Architecture

This guide explains how the toolkit is organised on disk and in namespaces, and the one rule that keeps it healthy: **dependencies only point down the layers**. 🧭

## The layers

From lowest to highest:

```text
Core < Infrastructure < Configuration < Hosting < Web < Build
```

| Layer | Owns | Projects |
|---|---|---|
| `Core` | Business abstractions, entities, value sets, application services, repository/service contracts, and the DI module contract (`IDependencyInjectionModule` and `UseModule`, in `Core/Composition/`) every higher project uses to expose its registrations. No I/O. | `Core`, `Core.Observability`, `Core.Events`, `Core.Messaging.Abstractions`, `Core.Persistence.Abstractions`, `Core.Health` |
| `Infrastructure` | Adapters to the outside world: databases, queues, HTTP clients, sinks, health reports, and the modules that register them. | `Infrastructure.*` |
| `Configuration` | Settings contracts, environment/secret handling, the observability bootstrap and the modules that register them. | `Configuration` |
| `Hosting` | Running a process: the `IStartup` contract and `UseStartup`, job/worker startups and entry points (Lambda and EC2), the worker pipeline and the workers' self-hosted health API. | `Hosting`, `Hosting.Workers` |
| `Web` | HTTP API and UI pipelines: startups, entry points, middleware, controllers, presentation contracts and DTOs. | `Web`, `Web.GraphQL`, `Web.Ui` |
| `Build` | NUKE build components. On top of the chain so that no shipped project can depend on build tooling. | `Build.Nuke` |

A project's layer is the **prefix of its name** (`src/<Layer>.<Name>/`, or just `src/Core/` for the base project). A project may use anything from its own layer or any layer **below** it, never above. For example, `Infrastructure.Persistence.Postgres` may use `Core` and `Core.*`, but no Core project may import an `Infrastructure.*`, `Configuration.*`, `Hosting.*`, `Web.*` or `Build.*` one.

The test kits (`Testing.*`) and the test projects under `tests/` (one per test kind, e.g. `tests/Unit/Unit.csproj`) sit outside the chain: they may reference any layer, but no production project may reference them. The same goes for [`samples/`](../../samples) (sample applications), which live outside `src/`.

## Inside a project

A project's own code sits **directly at its root**, organised by responsibility (`Entity/`, `Service/`, `Repository/`, `Extension/`, `DependencyInjection/`, ...), and namespaces follow folders:

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
| `Build.*` | any lower layer | `Build.Nuke/Core/Entity/Build/...` |

So `Infrastructure.Persistence.Oracle` may get a `Core/` folder if it has to expose its own domain entities, but code of its own layer, including its DI modules, goes at the root, never under `Infrastructure/` or `Configuration/`. Layer folders do not change the project's layer either: a `Core/` folder inside an `Infrastructure.*` project is still Infrastructure code, so it may only use projects up to Infrastructure.

## Dependency-injection modules

A project that has concrete services to register exposes them through one or more `IDependencyInjectionModule`s, under `DependencyInjection/Module/`. Not every project gets one: pure contracts, base classes meant to be subclassed and generic helpers have nothing to register.

| Project | Module | Notes |
|---|---|---|
| `Infrastructure.Persistence.Postgres` | `ModulePostgres(connectionOptions, resilienceOptions)`, `ModulePostgresHealthReport` | Connection string, keyed `writer`/`reader` pools and policies, retry builder, flow context, `IUnitOfWork`; reader/writer health reports. |
| `Infrastructure.Persistence.Oracle` | `ModuleOracle(connectionOptions, resilienceOptions)`, `ModuleOracleHealthReport` | Same shape as `ModulePostgres` / `ModulePostgresHealthReport`. |
| `Infrastructure.Messaging.Aws` | `ModuleSnsMessageBroker(options)`, `ModuleSqsMessageBroker(options)` | Keyed `topic` (SNS) and `queue` (SQS) publishers. |
| `Infrastructure.CorrelationId` | `ModuleCorrelationId(options?)` | Defaults unless a factory supplies options. |
| `Hosting.Workers` | `ModuleApplicationHealth(assembly)` | The workers' health endpoints (`/health/instance`, `/health/services`, `/health/status`) with the build metadata embedded in `assembly`. `AddServiceHealthCheckWorker(assembly)` registers them together with the `ServiceHealthCheckWorker` that serves them. |
| `Configuration` | `ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings>(observability?)`, `ModuleObservability(observerFactory?, correlationIdOptions?)`, `ModuleAppSettingsBase<TAppSettings>` | `ModuleLibConfiguration` registers the consumer's settings module (a `ModuleAppSettingsBase` subclass), `ModuleObservability` and the secret management in one go. |

Modules follow two rules:

1. **No hard-coded configuration.** A module never assumes a section name or a settings type. Options come in through constructor parameters, usually as a `Func<IConfiguration, TOptions>` evaluated when the module registers its services:

   ```csharp
   services.UseModule(
       new ModulePostgres(
           config => config.GetSection("Postgres").Get<PostgresConnectionOptions>()!,
           config => new PostgresResilienceOptions(config.GetValue<int>("Resilience:RetryCount"))),
       configuration,
       environment);
   ```

   Parameterless modules use `services.UseModule<TModule>(configuration, environment)`.
2. **Modules wrap, they don't replace.** Where a project already has an `Add*` extension (e.g. `AddSnsMessageBroker`), the module calls it, so consumers that don't use modules keep the same entry point.

`Core.*` projects ship plain `IServiceCollection` extensions instead of modules (e.g. `AddLocalEventBus()`).

## Where does a new file go?

1. **Pick the project by layer, then the folder by responsibility.** Code of the project's own layer goes at its root. Only code for an inner layer gets a layer folder: a repository contract is `Core/Service/Repository/...` even when it ships in an Infrastructure package (see `Infrastructure.Persistence.Db/Core/Service/Repository/Storage/Event/`).
2. **Extension methods on framework types** follow the extended type's namespace below `Extension/` (e.g. `Extension/Microsoft/DependencyInjection/`), and their namespace follows the folder like any other file (`Anarchy.Toolkit.Core.Events.Extension.Microsoft.DependencyInjection`), so consumers add the matching `using`.
3. **Embedded SQL** sits in a `Queries/` folder next to the repository that loads it, because resources are resolved from the repository's namespace (`<Namespace>.Queries.<Name>.sql`).
4. **A new project** must be named after its layer (`<Layer>.<Name>`); the layering check rejects unknown prefixes.
5. **`Composition/` vs `Configuration/`.** An application's composition root (its startup, entry point and DI wiring, e.g. `Web/Composition/WebAppStartupBase.cs` or a sample's `Composition/Startup.cs`) goes under `Composition/`. `Configuration/` is only for settings and options (e.g. `Entity/Configuration/.../PostgresConnectionOptions.cs`).

## Enforcing the rule

The `CheckLayering` NUKE target (`ILayeringBuild` in `src/Build.Nuke`) checks three things, using project-name prefixes by default:

1. **Project references**: no project references a project of a higher layer, and no production project references a `Testing.*` kit.
2. **File imports**: no source file imports a toolkit namespace owned by a project of a higher layer. This also catches upward dependencies that only reach a project transitively.
3. **Layer folders**: no project has a root folder named after its own layer or a higher one (see [Inside a project](#inside-a-project)).

```bash
make check_layering
```

For solutions organized by layer folders instead (e.g. `src/Core/Project1/Project1.csproj` and `src/Core/Project2/Project2.csproj`), use `make check_layering BY_FOLDER=true`. The first folder under `src/` determines the layer, including `src/Testing/` for test kits. Projects directly under `src/` or in an unknown layer folder fail. The mode applies to every project under `src/`: it never falls back to name prefixes, and the default mode never falls back to folders. Projects outside `src/` remain excluded. `LayeringLayers` and `GetLayer` can still be overridden for other conventions.

Set `--layering-root components` (or `make check_layering LAYERING_ROOT=components`) to use another root in either mode. The path is relative to the solution directory, or may be absolute; projects outside it are excluded.

Run it after moving code, adding folders or adding project references. To see the result for this toolkit's name-based convention, [`dependency-map.html`](../dependency-map.html) (`make dependency_map`) draws the project references by layer, with violations in red. The dependency map has its own grouping configuration.

## Namespace collisions to know about

The responsibility folders produce a few namespace segments that collide with type names:

- **`Event`.** `Core.Events` has both an `Event` entity and `.../Event/...` folders. In files under a namespace that has an `Event` child namespace, add an alias right after the file-scoped namespace line (usings placed there are resolved before parent namespaces):

  ```csharp
  namespace Anarchy.Toolkit.Core.Events.Service.Event.Domain;

  using Event = Anarchy.Toolkit.Core.Events.Entity.Event.Domain.Event;
  ```

- **`Attribute`.** Under `.../Entity/Attribute/` or `Presentation/Attribute/`, derive from `System.Attribute` explicitly.
- **`Host`.** A `Host/` folder (e.g. `Hosting/Host/`) can shadow `Microsoft.Extensions.Hosting.Host` for code in neighbouring namespaces. If you get `CS0118` on `Host.CreateDefaultBuilder(...)`, use the full type name.

The general fix for `CS0118` ("is a namespace but is used like a type") or `CS0104` (ambiguous reference) is the same: an alias placed right after the file-scoped namespace line.

Small, consistent structure makes it obvious where code belongs and makes layering drift visible in review. 💙
