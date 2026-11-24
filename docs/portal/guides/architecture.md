# Layered Architecture

This guide explains how the toolkit is organized on disk and in namespaces, and the rule that governs its dependencies: **dependencies only point down the layers**.

> **A guide, not a rule.** This page explains how the platform works. The conventions it mentions are recommendations: see [Layering](../conventions/layering.md) and [Dependency Injection](../conventions/dependency-injection.md) for the rules, why they exist and how to opt out.
{: .notice--info}

## The layers

From lowest to highest:

```text
Core < Infrastructure < Configuration < Hosting < Web < Automation
```

| Layer | Owns | Projects |
|---|---|---|
| `Core` | Business abstractions, entities, value sets, application services, repository/service contracts, and the DI module contract (`IDependencyInjectionModule` and `UseModule`, in `Core/Composition/`) every higher project uses to expose its registrations. No I/O. | `Core`, `Core.Telemetry`, `Core.Events`, `Core.Messaging.Abstractions`, `Core.Persistence.Abstractions`, `Core.Health` |
| `Infrastructure` | Adapters to the outside world: databases, queues, HTTP clients, correlation, logging-provider integrations, health reports, and the modules that register them. | `Infrastructure.*` |
| `Configuration` | Settings contracts, environment/secret handling, the telemetry bootstrap and the modules that register them. | `Configuration` |
| `Hosting` | Running a process: the `IStartup` contract and `UseStartup`, job/worker startups and entry points (Lambda and EC2), the worker pipeline and the workers' self-hosted health API. | `Hosting`, `Hosting.Workers` |
| `Web` | HTTP API and UI pipelines: startups, entry points, middleware, controllers, presentation contracts and DTOs. | `Web`, `Web.GraphQL`, `Web.Ui` |
| `Automation` | Build tooling: Anarchy automation components powered by [Fallout Build](https://fallout.build/), the command-line operations behind them and the `anarchy-toolkit` .NET tool. On top of the chain so that lower-layer projects cannot depend on build tooling. | `Automation.Build`, `Automation`, `Automation.Build.Cli` |

In the toolkit, a project's layer is the **prefix of its name** (`src/<Layer>.<Name>/`, or just `src/Core/` for the base project). `Automation` is the top layer by default. The toolkit opts into prefix mode in its root `.anarchy-toolkit.json`; adopters need not save any preferences. With no layering override, tools use the documented folder baseline (`src/Library`, standard application folders and layers), not a layout inferred from the repository. See [Layering](../conventions/layering.md#choose-one-project-layering-mode). A project may use anything from its own layer or any layer **below** it, never above. For example, `Infrastructure.Persistence.Db.Postgres` may use `Core` and `Core.*`, but no Core project may import an `Infrastructure.*`, `Configuration.*`, `Hosting.*`, `Web.*` or `Automation.*` one.

The test kits (`Testing.*`) and the test projects under `tests/` (one per test kind, e.g. `tests/Unit/Unit.csproj`) sit outside the chain: they may reference any layer, but no production project may reference them. The same goes for [`samples/`](../samples/README.md) (sample applications), which live outside `src/`.

### Package names in this monorepo

Because the toolkit uses prefix layering for independently published packages, its package names also encode their dependencies: within a layer, a package may only reference its **name ancestors** (`Web.MockServer.Ui` to `Web.MockServer` or `Web`; `Infrastructure.Persistence.Db.Postgres` to `Infrastructure.Persistence.Db`), and any lower layer. The few other same-layer references are listed with their reasons in `build/PackageHierarchy.cs`, and `make check_package_hierarchy` enforces the rule. It is specific to this repository: solutions that use folder-mode layering are never held to it.

`Infrastructure.Messaging.CloudEvents` references the lower-layer `Core.Events` envelopes, not the AWS transport or persistence/outbox packages. Its CloudEvents representation can therefore be used independently of either adapter.

Event-store bases and Outbox contracts and records are in `Infrastructure.Persistence.Db.Events`, which references `Infrastructure.Persistence.Db` (its name ancestor) and `Core.Events`. Applications implement their own Outbox repositories. The generic `Infrastructure.Persistence.Db` and provider adapters do not reference the events package. `Infrastructure.Persistence.Db` itself still references `Core.Events`: that is the generic local event bus used for transaction lifecycle notifications, not business-event coupling. The generic attribute-ID cache sits in `Core.Persistence.Abstractions` so any layer above can use it. See [ADR 0011](../decisions/0011-events-persistence-package.md).

## Inside a project

A project's code sits at its root, organized by responsibility folders (`Entity/`, `Service/`, `Repository/`, `DependencyInjection/`, ...), and namespaces follow folders. Folders named after a lower layer hold the classes a project introduces for that layer. The full rules, with the table of allowed layer folders, are in the [Layering convention](../conventions/layering.md#inside-a-project); [Where does a new file go?](../conventions/layering.md#where-does-a-new-file-go) answers the everyday question.

## Dependency-injection modules

A project that has concrete services to register exposes them through one or more `IDependencyInjectionModule`s, under `DependencyInjection/Module/`. Not every project gets one: pure contracts, base classes meant to be subclassed and generic helpers have nothing to register.

| Project | Module | Notes |
|---|---|---|
| `Infrastructure.Persistence.Db.Postgres` | `ModulePostgres(connectionOptions, resilienceOptions)`, `ModulePostgresHealthReport` | Connection string, keyed `writer`/`reader` pools and policies, retry builder, flow context, `IUnitOfWork`; reader/writer health reports. |
| `Infrastructure.Persistence.Db.Oracle` | `ModuleOracle(connectionOptions, resilienceOptions)`, `ModuleOracleHealthReport` | Same shape as `ModulePostgres` / `ModulePostgresHealthReport`. |
| `Infrastructure.Persistence.Db.Mysql` | `ModuleMysql(connectionOptions, resilienceOptions)`, `ModuleMysqlHealthReport` | Same shape as `ModulePostgres` / `ModulePostgresHealthReport`. |
| `Infrastructure.Messaging.Aws` | `ModuleSnsMessageBroker(options)`, `ModuleSqsMessageBroker(options)` | Keyed `topic` (SNS) and `queue` (SQS) publishers. |
| `Infrastructure.Messaging.Kafka` | `ModuleKafkaMessageBroker(options)` | Keyed `kafka` publisher. The application owns the consumer loop and offset policy. |
| `Infrastructure.CorrelationId` | `ModuleCorrelationId(options?)` | Defaults unless a factory supplies options. |
| `Infrastructure.Telemetry.Serilog` | `ModuleSerilogTelemetry(logger)` | Optional: Serilog as a logging provider (a `ModuleTelemetryBase`). |
| `Hosting.Workers` | `ModuleApplicationHealth(assembly)` | The workers' health endpoints (`/health/instance`, `/health/services`, `/health/status`) with the build metadata embedded in `assembly`. `AddServiceHealthCheckWorker(assembly)` registers them together with the `ServiceHealthCheckWorker` that serves them. |
| `Configuration` | `ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings>(telemetry?)`, `ModuleTelemetry(coreTelemetry?, correlationIdOptions?)`, `ModuleAppSettingsBase<TAppSettings>` | `ModuleLibConfiguration` registers the consumer's settings module (a `ModuleAppSettingsBase` subclass), `ModuleTelemetry` and the secret management in one go. `ModuleTelemetry` takes an optional `ModuleTelemetryBase` for the application's logging providers and metric recorder. |

How to write your own modules (no hard-coded configuration, wrap `Add*` extensions) is covered by the [Dependency Injection convention](../conventions/dependency-injection.md). `Core.*` projects ship plain [`IServiceCollection`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection) extensions instead of modules (e.g. `AddCoreTelemetry()`, `AddLocalEventBus()`); `Core.Telemetry` also offers `ModuleTelemetryBase`, the base for an application's own telemetry module.

## Enforcing the rule

The check is optional: leave `ILayeringBuild` out of your build, or adapt it as described in [Layering: opting out](../conventions/layering.md#opting-out-or-adapting).

The `CheckLayering` Anarchy target (`ILayeringBuild` in `src/Automation.Build`) resolves the layout from explicit Fallout parameters, saved `layering` overrides in `.anarchy-toolkit.json`, then the in-code folder baseline, and checks three things:

1. **Project references**: no project references a project of a higher layer, and no production project references a `Testing.*` kit.
2. **File imports**: no source file imports a toolkit namespace owned by a project of a higher layer. This also catches upward dependencies that only reach a project transitively.
3. **Layer folders**: no project has a root folder named after its own layer or a higher one (see [Inside a project](#inside-a-project)).

```bash
make check_layering
```

For solutions organized by layer folders instead (e.g. `src/Library/Core/Project1/Project1.csproj`), the baseline already uses folder mode and the standard `libraryRoot`; customize `layering.libraryRoot` and `layering.applicationFolders` in `.anarchy-toolkit.json` when needed. The first folder under the library root determines the layer, including `Testing/` for test kits, and application-folder mappings assign folders such as `src/Api` to a layer. Projects under `root` (default `src`) in neither, directly under the library root or in an unknown layer folder fail. Choose one mode for the entire solution: the check never falls back from folder to prefix detection or vice versa. Prefix mode suppresses folder-only fallbacks, while explicitly contradictory folder overrides are invalid. Projects outside `root` (tests, samples, the build project) are excluded. `GetLayer` can still be overridden for other conventions, though mixing modes that way is strongly discouraged.

Set `layering.root` to use another folder in either mode; paths are relative to the solution directory. The preferences file itself stays at the solution root; configured relative paths are solution-relative.

Run it after moving code, adding folders or adding project references.

## Dependency map

`make dependency_map` regenerates [`dependency-map.html`](../dependency-map.html), which draws the toolkit's project references by layer, with violations in red. It uses the same effective layering as the layering check. Regenerate and commit it whenever project references change, so the map below stays current.

<iframe src="../dependency-map.html" title="Anarchy.Toolkit dependency map" loading="lazy" style="width: 100%; height: 900px; border: 1px solid #dddcd7; border-radius: 4px;"></iframe>
