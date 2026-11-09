![logo](./docs/images/logo/anarchy-io-rect-384.png)

# Anarchy I/O - Toolkit

[![Publish NuGet packages](https://github.com/anarchy-io/anarchy-toolkit/actions/workflows/merge-publish-nuget.yml/badge.svg)](https://github.com/anarchy-io/anarchy-toolkit/actions/workflows/merge-publish-nuget.yml)

Reusable, self-contained NuGet packages for building .NET services.
Each package is an independent `dotnet` project in a single solution (`Anarchy.Toolkit.sln`) and is
published independently (per-project `dotnet pack`/`push`). Each package id is `Anarchy.Toolkit.<project name>`
(`src/Core.Events/` → `Anarchy.Toolkit.Core.Events`).

The toolkit is **standalone**: every dependency restores from nuget.org.

The repository follows a layered architecture, **Core < Infrastructure <
Configuration < Hosting < Web < Build**, where a project's layer is the prefix of its name (`src/<Layer>.<Name>/`, or `src/Core/`).
Dependencies only point down the layers, which `make check_layering` verifies. The `Testing.*` kits, the test
projects under `tests/` and the [`samples/`](samples) sit outside the chain. Projects with concrete
services expose them through their own `IDependencyInjectionModule`s, whose contract lives in `Core`.
See the [Layered Architecture guide](docs/guides/architecture.md).

Generic code is string/generic-parameterized; domain enums are replaced with `string` cores plus
generic enum overloads/factories that call `EnumExtension.ToDescription()`. Entities are marked with
`IEntity`/`IPersistedEntity`.

## Building

The repo ships a `make` + Nuke toolchain. Run `make help`
for the full, self-documenting list.

```bash
make install        # configure the local .NET environment, restore + compile; WITH_HOOKS=true installs the git hook
make install_dev_env # install/validate the SDK, set ASPNETCORE_ENVIRONMENT and trust development certificates
make install_sdk    # install or validate the SDK required by global.json
make appsettings    # generate untracked appsettings.Development.json from each appsettings.json.template (RESET=true re-asks every key)
make compile        # build the solution (Nuke Compile, warnings-as-errors)
make build          # clean + build
make lint           # dotnet format --verify-no-changes
make lint_fix       # dotnet format (auto-fix); AUTO_STAGE=true re-stages eligible fixes
make test           # run all test projects (unit/integration/end-to-end)
make test_unit      # unit tests only (COMPILE=true to rebuild first)
make list_projects  # list every project in the solution
make list_packages  # list every package id + version
make release_bump RELEASE_PROJECT=Core.Events RELEASE_BUMP=patch  # bump a package and its dependents
make pack           # pack every package into a fresh artifacts/packages/<run-id>/ directory
make publish        # pack + push to the NuGet feed (SOURCE=… required, API_KEY=… optional)
make upgrade_dependencies  # bump centrally-managed package versions (dotnet-outdated)
```

`make install` persists development settings in the shell environment file and trusts the local
HTTPS certificate; use `ENV=ci` to skip this setup. Dependency upgrades require confirmation;
`LOCK=Major` (default), `Minor`, or `None` controls the version lock. In non-interactive use,
`INSTALL_TOOL=true` and `UPGRADE=true` explicitly permit tool installation and upgrades respectively.
Auto-staging only considers files already staged with no unstaged changes before linting; partially
staged files are left for manual review. Avoid editing files concurrently with linting.

Git hooks can be previewed with `make git_hooks_enable OPTIONS="--dry-run --skip-test-runtime"`.
Replacing an existing hook requires `--force`. Use `make git_hooks_show` and `make git_hooks_disable`
to inspect and remove hooks.

You can also run arbitrary Nuke targets through Make:

```bash
make nuke TARGET=Pack                # e.g. Compile, Lint, TestAll, Pack, Publish, ListProjects
make nuke TARGET="Lint --IsCiBuild true"
make nuke_help                       # list all Nuke targets + parameters
```

Plain `dotnet` also works:

```bash
dotnet build Anarchy.Toolkit.sln -c Release
dotnet pack  Anarchy.Toolkit.sln -c Release -o ./artifacts/packages
```

### Tooling layout

- `Makefile` + `scripts/make/` — self-documenting Make targets and help (parsed from `#?`/`#+` markers).
- `scripts/bootstrap/` — SDK and development-environment installation and shell-profile helpers.
- `scripts/console/` and `scripts/shared/` — display utilities and shared colors.
- `scripts/build.nuke.sh` — reloads the shell env (fresh SDK) then runs the Nuke project.
- `build/` — the Nuke build project, see [Nuke build project](#nuke-build-project).
- `docs/guides/` — conventions ([index](docs/guides/README.md)): [architecture](docs/guides/architecture.md),
  [async](docs/guides/async-conventions.md), [enums](docs/guides/enum-conventions.md),
  [exceptions](docs/guides/exception-conventions.md),
  [endpoints](docs/guides/exposing-endpoints.md), [observability](docs/guides/observability.md),
  [UIs](docs/guides/building-uis.md), [testing](docs/guides/testing-conventions.md) and
  [NUKE builds](docs/guides/nuke-builds.md).
- `.nuke/parameters.json` — marks the repo root and pins `Solution`/`Configuration`.

> Tests run on **Microsoft.Testing.Platform** (`global.json` → `test.runner`).
> The Nuke test targets route xUnit v3 projects through MSBuild's `Test` target and any VSTest
> project through `dotnet test`.

### Nuke build project

`build/` is a thin consumer of the solution's own build components,
[`Anarchy.Toolkit.Build.Nuke`](docs/guides/nuke-builds.md) (`src/Build.Nuke`),
referenced as a project so the build always runs the code it
builds and packs:

- `Build.cs` — `partial class Build : NukeBuild, INuGetBuild`. All the standard targets come from the library:
  `Restore`, `Compile`, `Lint`/`LintFix`, `TestUnit`/`TestIntegration`/`TestEndToEnd`/`TestAll`, `Test` (unit
  only), `PullRequest`, `ListProjects`, `RunApp`/`RunApps`, `Clean`/`Build`. Test projects are matched by kind
  through their name segments (`Tests.Unit.*`, `*.Unit.Tests`, `*.Tests.Unit` or plain `Unit`).
- `INuGetBuild` provides `ListPackages`, `Pack` and dependency-ordered `Publish`; see the [NUKE guide](docs/guides/nuke-builds.md#nuget-packages).
- `Build.CodeGeneration.cs` — `GenerateTools`, regenerating the library's CLI tool wrappers from its specifications.
- `Directory.Build.targets` — imports the library's `buildTransitive` targets, which serve the NuGet.* assemblies
  from the installed SDK so the build keeps working on newer SDK feature bands (e.g. 10.0.400).

> Restoring needs nuget.org only. `nuget.config` declares the source + mapping; credentials for the
> publish feed come from the machine/CI config.

## Package catalogue

Every project under `src/` is one package, and its package id, assembly name and root namespace are all
`Anarchy.Toolkit.<project name>` (`src/Core.Events/` → `Anarchy.Toolkit.Core.Events`).
Each project has its own `README.md`, included in its NuGet package for display on NuGet.org.

Legend: **[Abs]** abstractions-only · **[Impl]** concrete implementation · dependencies list
sibling toolkit projects only (framework/NuGet deps omitted).

### `Core` layer — domain and application abstractions
| Package | Purpose | Depends on |
|---|---|---|
| `Anarchy.Toolkit.Core` | Generic helpers/extensions (`StringExtension`, `EnumExtension`, `DateHelper`, JSON converters, …), the exception primitives (`Exceptions/`), markers `IEntity`/`IPersistedEntity`/`IDeepCloneableEntity`, the keyed client-resolver pattern (`IClientResolver<T>`, `KeyedClientResolverBase<T>`), and the DI module contract (`IDependencyInjectionModule`, `UseModule<T>()` and `UseModule(module)` for modules with constructor parameters, in `Composition/`). | — |
| `Anarchy.Toolkit.Core.Observability` **[Impl]** | `Observer` facade (`Logger`, `MeasurerBase`, `LogContext`) with call-site logging/metrics, `Tags`, `DevelopmentLogger`, `LoggerMeasurer`, `CallSiteEntity` + `CalledFrom()`/`ParentCaller()`, generic `LogContext` extensions. | Core |
| `Anarchy.Toolkit.Core.Events` | In-process pub/sub (`ILocalEvent`, `ILocalEventHandler<T>`, `LocalEventBus` + `AddLocalEventBus`) and the generic event pipeline (`EventEnvelopeBase`, generic `Event`/`PersistedEvent`, processor, handler bus, `EventEnvelopeParser`). | Core, Core.Observability, Core.Messaging.Abstractions |
| `Anarchy.Toolkit.Core.Messaging.Abstractions` **[Abs]** | `IMessageBrokerPublisher`, in/out message entities, generic `OutboundMessageBuilder`. | Core |
| `Anarchy.Toolkit.Core.Persistence.Abstractions` **[Abs]** | UoW, async-local flow context, storage policy, repository contracts + exceptions. | Core |
| `Anarchy.Toolkit.Core.Health` **[Abs]** | The one health model every endpoint shares: service health reports (`IServiceHealthReport`, `ServiceHealthReportBase` for timed, exception-safe checks), `HealthCheckEntity`, `HealthCheckReport` (`HealthCheckReport.RunAsync(reports)` runs them in parallel), `ExceptionReport`; plus `IBuildMetadata` and `ApplicationRunbookUriAttribute`. | Core |

### `Infrastructure` layer — adapters to external systems
| Package | Purpose | Depends on |
|---|---|---|
| `Anarchy.Toolkit.Infrastructure.Http` | HTTP repository/client plumbing: `HttpRepositoryBase`/`ScopedHttpRepositoryBase`, handlers (timeout, MSAL certificate auth), client configuration. | Core, Core.Observability |
| `Anarchy.Toolkit.Infrastructure.Persistence.Db` **[Impl]** | Provider-agnostic relational layer: `IDbDialect` (static-abstract provider contract), UoW, connection pool base, (resilience) policy, retry builder, connection-string/options bases, repository bases (`Db{Reader,Writer,BatchWriter}RepositoryBase<TConnection, TDialect, …>`), `AttributeIdCacheBase`, `DescriptionEnumTypeHandler`, and DB health reports (`DbHealthReportBase<TConnection, TDialect>` runs the dialect's `HealthCheckSql`, test named `{dialect}_connection-{reader\|writer}`; `AddDbHealthReports<TReaderReport, TWriterReport>()`), and the provider-agnostic transactional outbox / event store (repository contracts under `Core/`, records, type-table cache, rollback handler, and `Db{TaskOutboxReader,TaskOutboxWriter,DomainEventReader,EventWriter}RepositoryBase<TConnection, TDialect>`). Options: `DbConnectionOptions`, `DbResilienceOptions`. | Core, Core.Observability, Core.Persistence.Abstractions, Core.Events, Core.Health |
| `Anarchy.Toolkit.Infrastructure.Persistence.Postgres` **[Impl]** | Npgsql adapter: `PostgresDbDialect`, connection string/pool/envelope, `PostgresDbUnitOfWork`, retry builder, resilience policy, and `PostgresDb*RepositoryBase` aliases of the Db bases, plus Postgres health reports (`AddPostgresHealthReports`, `ModulePostgresHealthReport`), and `ModulePostgres`. Options: `PostgresConnectionOptions`, `PostgresResilienceOptions`. | Core, Infrastructure.Persistence.Db |
| `Anarchy.Toolkit.Infrastructure.Persistence.Oracle` **[Impl]** | ODP.NET (`Oracle.ManagedDataAccess.Core`) adapter: `OracleDbDialect`, connection string (full connect descriptor with `EXPIRE_TIME` keepalive)/pool (per-connection command timeout + session time zone)/envelope, `OracleDbUnitOfWork`, retry builder, resilience policy, and `OracleDb*RepositoryBase` aliases of the Db bases, plus Oracle health reports (`AddOracleHealthReports`, `ModuleOracleHealthReport`; `SELECT SYSDATE FROM DUAL`, tests named `oracle_connection-{reader\|writer}`), and `ModuleOracle`. Options: `OracleConnectionOptions` (`DbName` = service name, port defaults to 1521), `OracleResilienceOptions`. | Core, Infrastructure.Persistence.Db |
| `Anarchy.Toolkit.Infrastructure.Persistence.Mysql` **[Impl]** | MySqlConnector adapter: `MysqlDbDialect`, connection string/pool, unit of work, retry policy, repository bases, health reports and `ModuleMysql`. Port defaults to 3306; session time zones require MySQL's time-zone tables for named zones. Batch writes use Dapper execute-many. | Core, Infrastructure.Persistence.Db |
| `Anarchy.Toolkit.Infrastructure.Messaging.Aws` **[Impl]** | SNS and SQS publishers (`AddSnsMessageBroker`, `AddSqsMessageBroker`, `ModuleSnsMessageBroker`, `ModuleSqsMessageBroker`) over a shared AWS publisher base + request-builder base, inbound mapper, compression. Options: `AwsMessagingOptions`. | Core, Core.Observability, Core.Messaging.Abstractions |
| `Anarchy.Toolkit.Infrastructure.Observability.Serilog` **[Impl]** | Standard log-property enrichers with overridable names (`WithTeam`, `WithAppGroup`, `WithCostCenter`, `WithCostCenterFromEnvironment`, `WithLogRetention`), `WithAwsEc2InstanceId` and the `Ec2CloudWatch(logGroup)` sink. | Core |
| `Anarchy.Toolkit.Infrastructure.CorrelationId` **[Impl]** | Async-local `CorrelationContext`, ASP.NET Core `UseCorrelationId()` middleware (configurable headers, `X-Correlation-Id`/`X-Attempt-Id` by default), Serilog `WithCorrelationId()` enricher, `ModuleCorrelationId`. | Core |

### `Configuration` layer — settings, secrets and observability bootstrap
| Package | Purpose | Depends on |
|---|---|---|
| `Anarchy.Toolkit.Configuration` | One capability interface per settings section (`IAuthServiceAppSettings`, `IOAuthAppSettings`, `IAwsAppSettings`, `IPostgresAppSettings`, `IOracleAppSettings`, `IResilienceAppSettings`, `IWorkerHealthCheckAppSettings`, all deriving from `IAppSettings`) implemented by the consumer's own settings type, bound by `ModuleAppSettingsBase<TAppSettings>` (override `RegisterAdditionalTypesForSettings` to expose each interface in DI), secrets (`Kms`/`Cached`/`InMemory` services), vendor-neutral observability bootstrap (`ModuleObservability` + an overridable `ObserverFactory`), `ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings>` (settings + observability + secrets in one module), config extensions (`AddAwsSystemsManagerParameterStore(environment, prefix)`, called by the application). | Core, Core.Observability, Infrastructure.CorrelationId |

### `Hosting` layer — running a process
| Package | Purpose | Depends on |
|---|---|---|
| `Anarchy.Toolkit.Hosting` | The `IStartup` contract and `UseStartup<TStartup>(appRunnerType)`, generic startup + entry points for jobs and workers (`JobStartupBase`, `JobEntryPointBase`, `WorkerEntryPointBase`, `AppHostBuilder`) with an overridable `ConfigureAppConfiguration` hook (configuration sources such as the Parameter Store are the application's choice); `DiFactory<TAppSettingsDiModule, TAppSettings>.Build(...)`. | Core, Configuration |
| `Anarchy.Toolkit.Hosting.Workers` **[Impl]** | `WorkerBase`, `MonitoredWorkerBase`, `ResilientWorkerBase`, `HeartbeatWorker`, and the workers' health API: `ServiceHealthCheckWorker` (`AddServiceHealthCheckWorker(assembly)`) self-hosts `/health/instance`, `/health/services` (the registered `IServiceHealthReport`s as a `HealthCheckReport`, same JSON as the web API's health controller; 500 when a test fails) and `/health/status` (build metadata), via `SelfHostedApplicationHealthApi`, the ASP.NET Core middleware and `ModuleApplicationHealth`. | Core, Configuration, Core.Observability, Core.Health |
### `Web` layer — API and UI pipelines
| Package | Purpose | Depends on |
|---|---|---|
| `Anarchy.Toolkit.Web` | REST API pipeline: startup/entry-point bases (`RestApiWebAppStartupBase`, `WebAppEntryPoint`, `AwsWebAppEntryPointBase`), middleware, auth (generic scopes via params), swagger, JSON converters, validation attributes, health controller. | Hosting, Infrastructure.Http, Core.Health, Core.Persistence.Abstractions |
| `Anarchy.Toolkit.Web.GraphQL` | Generic GraphQL wiring (`AddToolkitGraphQl<TSchema>`, `UseToolkitGraphQl<TSchema>`, `SchemaBase`, `GraphQlRoutes`). | Web |
| `Anarchy.Toolkit.Web.Ui` | Interactive-server Blazor UI pipeline: `UiWebAppStartupBase`, cookie-session OAuth login against a consumer-supplied environment catalog, `UiApiClientBase` for downstream APIs, and reusable components (notifications, confirmation modals, JSON viewer, pagination, login form). See the [Building UIs guide](docs/guides/building-uis.md). | Web |

### `Build` layer — build tooling (top of the chain: nothing ships with a dependency on it)
| Package | Purpose | Depends on |
|---|---|---|
| `Anarchy.Toolkit.Build.Nuke` | NUKE build components in `Component/` (`IDefaultBuild`, `IVersionedBuild`, `IStandardBuild`, `IContainerBuild`, `ICodeDeployBuild`, `IAwsLambdaBuild`, `IFullReleaseBuild`, `IHasEndToEndTests`, `IReportFailureOnSlack`), MTP-aware test runner, KMS secrets, user secrets. See the [NUKE builds guide](docs/guides/nuke-builds.md). | — |

### `Testing` kits — packable test helpers (outside the layer chain)
| Package | Purpose | Depends on |
|---|---|---|
| `Anarchy.Toolkit.Testing` | `TestCaseBase` / `TestCaseBase<TAppSettings>`, `TestHelper`, `InMemoryObserver`, `EntityFixtureBase`, API-gateway assertions (AwesomeAssertions), in-memory metrics + Serilog sink with assertions (`Helper.Observability.Monitoring`). | Core, Core.Observability, Configuration |
| `Anarchy.Toolkit.Testing.Db` | `DbTestHelper` (`RegisterEnumTypeHandler<TEnum>()`, `TruncateTableAsync<TConnection, TDialect>`, `ClearTableAsync`) + `DbOptionsMapper`. | Testing, Infrastructure.Persistence.Db |
| `Anarchy.Toolkit.Testing.Postgres` | `PostgresHelper`/`SingletonPostgresHelper` + `PostgresOptionsMapper`, generic `RegisterEnumTypeHandler<TEnum>()`. | Testing.Db, Infrastructure.Persistence.Postgres |
| `Anarchy.Toolkit.Testing.Oracle` | `OracleHelper`/`SingletonOracleHelper` + `OracleOptionsMapper`, generic `RegisterEnumTypeHandler<TEnum>()`. | Testing.Db, Infrastructure.Persistence.Oracle |
| `Anarchy.Toolkit.Testing.Mysql` | `MysqlHelper`/`SingletonMysqlHelper` + `MysqlOptionsMapper`, `IMysqlAppSettings`, enum handlers and table cleanup. MySQL `TRUNCATE` resets auto-increment and may fail for referenced tables. | Testing.Db, Infrastructure.Persistence.Mysql |
| `Anarchy.Toolkit.Testing.Aws` | LocalStack SNS/SQS helpers (`SqsHelper`, `AwsMessageBrokerHelper`), `SqsMessageFixture` (string-named topics/queues). | Testing, Infrastructure.Messaging.Aws |
| `Anarchy.Toolkit.Testing.Web` | `RestApiTestCaseBase<TStartup, TAppSettings>` (native + Lambda), `TestableLambdaEntrypoint`, generic `MockAuthorizationToken<TAppSettings>(scopes)`. | Testing, Web |

## Samples and company extras

Company-specific behaviour is not part of the toolkit: organisations plug their conventions into its extension points (an overridable `ObserverFactory`, options and constructor parameters, `ConfigureAppConfiguration` hooks). [`samples/`](samples) holds sample applications (REST API, Lambda job, EC2 worker, back-office UI) built on the toolkit's defaults.

## Consuming a package

Each impl package ships a DI registration entry point (either an `IDependencyInjectionModule`
or a `ServiceCollection` extension), so consumers bind classes without manual wiring, e.g.:

```csharp
services.AddLocalEventBus();
services.AddSnsMessageBroker(new AwsMessagingOptions("eu-west-1", "123456789012"));
services.AddSqsMessageBroker(new AwsMessagingOptions("eu-west-1", "123456789012"));
services.AddPostgresHealthReports();

// or, as modules (options come from the application's configuration, never a hard-coded section):
services
    .UseModule(new ModuleSnsMessageBroker(config => new AwsMessagingOptions(config["Aws:Region"]!, config["Aws:AccountId"]!)),
        configuration, environment)
    .UseModule<ModulePostgresHealthReport>(configuration, environment);
```

See [Dependency-injection modules](docs/guides/architecture.md#dependency-injection-modules) for the modules each package ships.

### MySQL

Register `ModuleMysql` with an `Observer` and factories for `MysqlConnectionOptions` and
`MysqlResilienceOptions`; `AddMysqlHealthReports()` registers the reader and writer checks. The unkeyed
`IStoragePolicy<MySqlConnection>` uses the writer; use the `reader` key for read-only operations.
Hosts may be absolute URIs such as `tcp://mysql.example` or relative host names. Port defaults to 3306,
and the command timeout is in seconds. A nonempty `TimeZone` is applied to each newly opened session;
named zones such as `UTC` require populated MySQL time-zone tables, while offsets such as `+00:00` do not.

The adapter enables MySqlConnector's `Ignore Command Transaction` option because the shared repositories
execute commands on the unit of work's connection without passing its transaction explicitly. Individual
commands are not retried inside a transaction: a deadlock may invalidate the entire transaction, so retry
the whole unit of work instead. Provide MySQL-specific SQL for repositories and outbox implementations.
`Testing.Mysql` provides `IMysqlAppSettings` in `Anarchy.Toolkit.Testing.Mysql.Entity.Configuration` for
the settings-based fixtures; table truncation resets auto-increment and may fail on foreign-key references.

## Adding a database provider

`Infrastructure.Persistence.Db` and `Testing.Db` hold everything that is not provider-specific; a provider
(Postgres, Oracle and MySQL today) only adds a thin adapter, mirroring the existing provider projects:

1. **Infrastructure.Persistence.\<Provider\>** — a dialect implementing `IDbDialect` (`Name`, `IsNoData`, `IsUniqueViolation`,
   plus overrides of the ANSI defaults that differ: `IsServerError`, `IsTransient`, savepoint SQL — return `null`
   from `SavepointReleaseSql` when the provider has no `RELEASE SAVEPOINT` —, `ToBatchParameters`,
   `HealthCheckSql`, `TruncateTableSql`), a connection string (`DbConnectionStringBase<TOptions>`),
   a pool (`DbConnectionPoolBase<TConnection>.CreateConnection`), and one-line UoW / policy / retry-builder /
   repository-base subclasses closing the generics over the provider's connection and dialect, plus a
   `Module<Provider>` under `DependencyInjection/Module/`.
2. **Outbox, outside the toolkit** — the toolkit ships only the `Db*RepositoryBase<TConnection, TDialect>` outbox
   bases. Consumers (or an adapters package) add keyed-DI concretes over them, with the provider's SQL in a
   `Queries/` folder next to them (`LoadSql` resolves from the most derived type's namespace). Override
   `IdSequenceName` if the sequences are named differently.
3. **Health, in Infrastructure.Persistence.\<Provider\>** — reader/writer subclasses of `DbHealthReportBase`, an
   `Add<Provider>HealthReports()` extension and a `Module<Provider>HealthReport`.
4. **Testing.\<Provider\>** — helpers delegating to `DbTestHelper`.

## Notes & follow-ups

- **Independent versioning:** every project carries its own `<Version>`; all start at `1.0.0` for the first release. A
  `ProjectReference` between toolkit projects is emitted as a NuGet `<dependency>` on pack, so the
  in-repo dev loop and published dependency metadata both work without `PackageReference` juggling.
- **Oracle specifics:** SQL files must not end with `;` (ODP.NET rejects it) and use `:Name` binds (Dapper turns
  on `BindByName`). The outbox get-or-insert of type ids runs `Common.<Type>.Insert.IfMissing` (`MERGE`) then
  `Common.<Type>.Get.ByName`, as Oracle cannot select from an `INSERT` (`IDbDialect.CanSelectFromInsert`). Ids are
  `CAST(… AS NUMBER(9|18))` so ODP.NET returns `int`/`long`. Savepoints are never released (no such statement).
  `TruncateTableAsync` uses plain `TRUNCATE TABLE` (no identity restart / cascade).
- **Known persistence gaps** (pre-existing, carried over by the Db extraction):
  `GetLatestSequenceValueAsync` loads `Common.LatestSequenceValue.Get.sql`, which no package ships yet;
  the outbox type-id cache is shared by every `AttributeIdCacheBase<EventTypeTable, int>` subclass, so two providers
  in one process would share type ids; and `DbRepositoryBase`'s SQL cache is keyed by file name only (per
  connection type), so two repositories overriding the same query name collide.
- **`Anarchy.Toolkit.Web.Ui`** holds only domain-free UI plumbing; API clients, pages and the concrete
  environment URLs belong to the consumer (supplied through `UiAuthenticationOptions.Environments`).
- **Domain code** (task orchestration, concrete repositories and clients, domain event payloads, message
  destinations, swagger scopes, UI environment URLs) intentionally stays in consuming applications.
- **Assertions:** the toolkit uses **AwesomeAssertions** rather than FluentAssertions;
  only the `Anarchy.Toolkit.Testing*` packages depend on it (verified on the packed nuspecs).
- **Tests:** test projects are never published, so there is one per test kind: `tests/Unit/Unit.csproj` holds
  every unit test, in one folder per `src/` project whose dotted name becomes nested folders
  (`src/Core.Events` → `tests/Unit/Core/Events/`), with test classes mirroring the package's folders
  (xunit.v3 on Microsoft.Testing.Platform, AwesomeAssertions, Moq; shared settings in
  `tests/Directory.Build.props`), with domain types replaced by test-local dummies.
  `make test_unit` runs them; there are no integration/end-to-end projects yet (they would need
  Postgres/LocalStack and a schema).
