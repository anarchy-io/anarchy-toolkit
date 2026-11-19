# Package Catalog

The packages are the **Infrastructure** and **Build Ecosystem** pillars of the [Anarchy I/O Toolkit platform](../README.md#the-three-pillars), each adoptable on its own. Every project under `src/` is one package. Its package ID, assembly name and root namespace are all `Anarchy.Toolkit.<project name>` (`src/Core.Events/` → `Anarchy.Toolkit.Core.Events`). Each package's README, linked below, is also displayed on NuGet.org.

![Stacked modular blocks forming layers, flat outline illustration, dark background with red-orange accent, wide banner](../assets/images/headers/packages.png)

> **New here?** The [`anarchy-toolkit` CLI tool](Build.Cli.Tool/README.md) and the [`Build.Fallout`](Build.Fallout/README.md) build components are the quickest way in; see [Getting Started](../getting-started/README.md).
{: .notice--primary}

Packages are grouped by [layer](../guides/architecture.md): a package may depend only on its own layer or the layers below it. The [dependency map](../guides/architecture.md#dependency-map) draws these references.

Every package is published to NuGet.org under the [`anarchy-io` profile](https://www.nuget.org/profiles/anarchy-io); the 📦 next to each name opens its NuGet.org page.

**Legend:** **[Abs]** abstractions only · **[Impl]** concrete implementation. *Depends on* lists sibling toolkit packages only (framework and NuGet dependencies are omitted).

## Core: domain and application abstractions

| Package | Purpose | Depends on |
|---|---|---|
| [`Core`](Core/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Core "Anarchy.Toolkit.Core on NuGet.org") | Generic helpers and extensions (`StringExtension`, `EnumExtension`, `DateHelper`, JSON converters, …), the exception primitives, the `IEntity`/`IPersistedEntity`/`IDeepCloneableEntity` markers, the keyed client-resolver pattern (`IClientResolver<T>`, `KeyedClientResolverBase<T>`) and the DI module contract (`IDependencyInjectionModule`, `UseModule<T>()` and `UseModule(module)`). | — |
| [`Core.Observability`](Core.Observability/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Core.Observability "Anarchy.Toolkit.Core.Observability on NuGet.org") **[Impl]** | The `Observer` facade (`Logger`, `MeasurerBase`, `LogContext`) with call-site logging and metrics, `Tags`, `DevelopmentLogger`, `LoggerMeasurer`, `CallSiteEntity` + `CalledFrom()`/`ParentCaller()` and generic `LogContext` extensions. | Core |
| [`Core.Telemetry`](Core.Telemetry/README.md) **[Impl]** | Provider-neutral `IAppTelemetry` facade over Microsoft logging and standard .NET metric instruments. Existing packages still use `Core.Observability` during migration. | Core |
| [`Core.Events`](Core.Events/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Core.Events "Anarchy.Toolkit.Core.Events on NuGet.org") | In-process pub/sub (`ILocalEvent`, `ILocalEventHandler<T>`, `LocalEventBus` + `AddLocalEventBus`) and the generic event pipeline (`EventEnvelopeBase`, generic `Event`/`PersistedEvent`, processor, handler bus, `EventEnvelopeParser`). | Core, Core.Telemetry, Core.Messaging.Abstractions |
| [`Core.Messaging.Abstractions`](Core.Messaging.Abstractions/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Core.Messaging.Abstractions "Anarchy.Toolkit.Core.Messaging.Abstractions on NuGet.org") **[Abs]** | `IMessageBrokerPublisher`, inbound/outbound message entities and the generic `OutboundMessageBuilder`. | Core |
| [`Core.Persistence.Abstractions`](Core.Persistence.Abstractions/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Core.Persistence.Abstractions "Anarchy.Toolkit.Core.Persistence.Abstractions on NuGet.org") **[Abs]** | Unit of work, async-local flow context, storage policy, repository contracts and exceptions. | Core |
| [`Core.Health`](Core.Health/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Core.Health "Anarchy.Toolkit.Core.Health on NuGet.org") **[Abs]** | The health model every endpoint shares: service health reports (`IServiceHealthReport`, `ServiceHealthReportBase` for timed, exception-safe checks), `HealthCheckEntity`, `HealthCheckReport` (`RunAsync(reports)` runs them in parallel) and `ExceptionReport`; plus `IBuildMetadata` and `ApplicationRunbookUriAttribute`. | Core |

## Infrastructure: adapters to external systems

| Package | Purpose | Depends on |
|---|---|---|
| [`Infrastructure.Http`](Infrastructure.Http/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Http "Anarchy.Toolkit.Infrastructure.Http on NuGet.org") | HTTP repository and client plumbing: `HttpRepositoryBase`/`ScopedHttpRepositoryBase`, handlers (timeout, MSAL certificate authentication) and client configuration. | Core, Core.Telemetry |
| [`Infrastructure.Persistence.Db`](Infrastructure.Persistence.Db/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Db "Anarchy.Toolkit.Infrastructure.Persistence.Db on NuGet.org") **[Impl]** | The provider-agnostic relational layer: the `IDbDialect` provider contract, unit of work, connection pool, resilience policy and retry builder, connection-string and options bases, repository bases (`Db{Reader,Writer,BatchWriter}RepositoryBase<TConnection, TDialect, …>`), `AttributeIdCacheBase`, `DescriptionEnumTypeHandler`, database health reports and the transactional outbox/event store bases. See [Database Providers](../guides/database-providers.md). | Core, Core.Observability, Core.Persistence.Abstractions, Core.Events, Core.Health |
| [`Infrastructure.Persistence.Postgres`](Infrastructure.Persistence.Postgres/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Postgres "Anarchy.Toolkit.Infrastructure.Persistence.Postgres on NuGet.org") **[Impl]** | Npgsql adapter: `PostgresDbDialect`, connection string, pool, unit of work, resilience, `PostgresDb*RepositoryBase` aliases, health reports and `ModulePostgres`. | Core, Infrastructure.Persistence.Db |
| [`Infrastructure.Persistence.Oracle`](Infrastructure.Persistence.Oracle/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Oracle "Anarchy.Toolkit.Infrastructure.Persistence.Oracle on NuGet.org") **[Impl]** | ODP.NET (`Oracle.ManagedDataAccess.Core`) adapter: `OracleDbDialect`, connect-descriptor connection string, pool with per-connection command timeout and session time zone, unit of work, resilience, `OracleDb*RepositoryBase` aliases, health reports and `ModuleOracle`. | Core, Infrastructure.Persistence.Db |
| [`Infrastructure.Persistence.Mysql`](Infrastructure.Persistence.Mysql/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Persistence.Mysql "Anarchy.Toolkit.Infrastructure.Persistence.Mysql on NuGet.org") **[Impl]** | MySqlConnector adapter: `MysqlDbDialect`, connection string, pool, unit of work, resilience, repository bases, health reports and `ModuleMysql`. | Core, Infrastructure.Persistence.Db |
| [`Infrastructure.Messaging.Aws`](Infrastructure.Messaging.Aws/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Messaging.Aws "Anarchy.Toolkit.Infrastructure.Messaging.Aws on NuGet.org") **[Impl]** | SNS and SQS publishers (`AddSnsMessageBroker`, `AddSqsMessageBroker` and their modules) over shared publisher and request-builder bases, an inbound mapper and compression. | Core, Core.Telemetry, Core.Messaging.Abstractions |
| [`Infrastructure.Observability.Serilog`](Infrastructure.Observability.Serilog/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.Observability.Serilog "Anarchy.Toolkit.Infrastructure.Observability.Serilog on NuGet.org") **[Impl]** | Standard log-property enrichers with overridable names (`WithTeam`, `WithAppGroup`, `WithCostCenter`, `WithLogRetention`, …), `WithAwsEc2InstanceId` and the `Ec2CloudWatch(logGroup)` sink. | Core |
| [`Infrastructure.Telemetry.Serilog`](Infrastructure.Telemetry.Serilog/README.md) **[Impl]** | Optional Serilog provider for `Core.Telemetry`, configured with an application-owned logger and sinks. | Core.Telemetry |
| [`Infrastructure.CorrelationId`](Infrastructure.CorrelationId/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Infrastructure.CorrelationId "Anarchy.Toolkit.Infrastructure.CorrelationId on NuGet.org") **[Impl]** | Async-local `CorrelationContext`, the `UseCorrelationId()` ASP.NET Core middleware (`X-Correlation-Id`/`X-Attempt-Id` headers by default), the Serilog `WithCorrelationId()` enricher and `ModuleCorrelationId`. | Core |

## Configuration: settings, secrets and observability bootstrap

| Package | Purpose | Depends on |
|---|---|---|
| [`Configuration`](Configuration/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Configuration "Anarchy.Toolkit.Configuration on NuGet.org") | Settings and secrets, provider-neutral `ModuleTelemetry`, and the legacy `ModuleObservability`/`ObserverFactory` bootstrap; `ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings>` combines them. | Core, Core.Telemetry, Core.Observability, Infrastructure.CorrelationId |

## Hosting: running a process

| Package | Purpose | Depends on |
|---|---|---|
| [`Hosting`](Hosting/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Hosting "Anarchy.Toolkit.Hosting on NuGet.org") | The `IStartup` contract and `UseStartup<TStartup>(appRunnerType)`, startups and entry points for jobs and workers (`JobStartupBase`, `JobEntryPointBase`, `WorkerEntryPointBase`, `AppHostBuilder`) with an overridable `ConfigureAppConfiguration` hook, and `DiFactory<TAppSettingsDiModule, TAppSettings>.Build(...)`. | Core, Configuration |
| [`Hosting.Workers`](Hosting.Workers/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Hosting.Workers "Anarchy.Toolkit.Hosting.Workers on NuGet.org") **[Impl]** | `WorkerBase`, `MonitoredWorkerBase`, `ResilientWorkerBase`, `HeartbeatWorker` and the workers' self-hosted health API (`/health/instance`, `/health/services`, `/health/status`) through `AddServiceHealthCheckWorker(assembly)` or `ModuleApplicationHealth`. | Core, Configuration, Core.Observability, Core.Health |

## Web: API and UI pipelines

| Package | Purpose | Depends on |
|---|---|---|
| [`Web`](Web/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Web "Anarchy.Toolkit.Web on NuGet.org") | REST API pipeline: startups and entry points (`RestApiWebAppStartupBase`, `WebAppEntryPoint`, `AwsWebAppEntryPointBase`), middleware, authentication with generic scopes, Swagger, JSON converters, validation attributes and the health controller. | Hosting, Infrastructure.Http, Core.Health, Core.Persistence.Abstractions |
| [`Web.GraphQL`](Web.GraphQL/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Web.GraphQL "Anarchy.Toolkit.Web.GraphQL on NuGet.org") | Generic GraphQL wiring (`AddToolkitGraphQl<TSchema>`, `UseToolkitGraphQl<TSchema>`, `SchemaBase`, `GraphQlRoutes`). | Web |
| [`Web.Ui`](Web.Ui/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Web.Ui "Anarchy.Toolkit.Web.Ui on NuGet.org") | Interactive-server Blazor pipeline: `AddToolkitUi`/`UseToolkitUi` on any host or `UiWebAppStartupBase`, an opt-in cookie-session OAuth login against an application-supplied environment catalog, `UiApiClientBase` for downstream APIs and reusable components. Only domain-free plumbing: pages, API clients and environment URLs belong to the application. See [Building UIs](../guides/building-uis.md). | Web |
| [`Web.MockServer`](Web.MockServer/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Web.MockServer "Anarchy.Toolkit.Web.MockServer on NuGet.org") | HTTP mock server for integration and end-to-end tests: MockServer-compatible JSON expectations, consumer middlewares (`MockServerEntryPoint.RunAsync(args, …)`), request recording to JSON lines, a `/__mock` control API, verbosity levels and port `6001` or the next free one. Run by `IMockServerBuild`, by hand, or in-process with `MockServerHost`. Keeps an in-memory log (`/__mock/logs`) and lists its middlewares (`/__mock/middlewares`). | Core |
| [`Web.MockServer.Ui`](Web.MockServer.Ui/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Web.MockServer.Ui "Anarchy.Toolkit.Web.MockServer.Ui on NuGet.org") | Browser UI for the mock server at `/__mock/ui`, built on `Web.Ui`: loaded expectations (with reload), received requests, plugged-in middlewares and a live log. Standalone executable, or `UseUi()` in a host of your own; `--no-ui` (or `UI=false` with the Make targets) turns it off. | Web.MockServer, Web.Ui |

## Build: build tooling

At the top of the chain, so nothing that ships can depend on it.

| Package | Purpose | Depends on |
|---|---|---|
| [`Build.Fallout`](Build.Fallout/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Build.Fallout "Anarchy.Toolkit.Build.Fallout on NuGet.org") | Fallout build components, the MTP-aware test runner, deployment, secrets and local automation. See [Fallout Builds](../guides/fallout-builds.md). | Build.Cli |
| [`Build.Cli`](Build.Cli/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Build.Cli "Anarchy.Toolkit.Build.Cli on NuGet.org") | The build and command-line operations behind Fallout and the tool: scaffolding, layering, release versioning and notes, Git hooks, and the distributed Makefile and scripts. | — |
| [`Build.Cli.Tool`](Build.Cli.Tool/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Build.Cli.Tool "Anarchy.Toolkit.Build.Cli.Tool on NuGet.org") | The `anarchy-toolkit` .NET global tool: `bootstrap` solutions with configurable layering and `scaffold` APIs, jobs and UIs into them. | Build.Cli (bundled) |

`Anarchy.Toolkit.Build.Nuke` is deprecated and no longer maintained; migrate to `Anarchy.Toolkit.Build.Fallout`.

## Testing kits: reusable test helpers

Outside the layer chain: they may reference any layer, but no production package may reference them. See [Testing Conventions](../conventions/testing.md).

| Package | Purpose | Depends on |
|---|---|---|
| [`Testing`](Testing/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Testing "Anarchy.Toolkit.Testing on NuGet.org") | `TestCaseBase` / `TestCaseBase<TAppSettings>`, `TestHelper`, `InMemoryObserver`, `EntityFixtureBase`, API Gateway assertions, and an in-memory metrics + Serilog sink with assertions. | Core, Core.Observability, Configuration |
| [`Testing.Db`](Testing.Db/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Db "Anarchy.Toolkit.Testing.Db on NuGet.org") | `DbTestHelper` (`RegisterEnumTypeHandler<TEnum>()`, `TruncateTableAsync<TConnection, TDialect>`, `ClearTableAsync`) and `DbOptionsMapper`. | Testing, Infrastructure.Persistence.Db |
| [`Testing.Postgres`](Testing.Postgres/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Postgres "Anarchy.Toolkit.Testing.Postgres on NuGet.org") | `PostgresHelper`/`SingletonPostgresHelper`, `PostgresOptionsMapper` and enum type handlers. | Testing.Db, Infrastructure.Persistence.Postgres |
| [`Testing.Oracle`](Testing.Oracle/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Oracle "Anarchy.Toolkit.Testing.Oracle on NuGet.org") | `OracleHelper`/`SingletonOracleHelper`, `OracleOptionsMapper` and enum type handlers. | Testing.Db, Infrastructure.Persistence.Oracle |
| [`Testing.Mysql`](Testing.Mysql/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Mysql "Anarchy.Toolkit.Testing.Mysql on NuGet.org") | `MysqlHelper`/`SingletonMysqlHelper`, `MysqlOptionsMapper`, `IMysqlAppSettings`, enum type handlers and table cleanup. | Testing.Db, Infrastructure.Persistence.Mysql |
| [`Testing.Aws`](Testing.Aws/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Aws "Anarchy.Toolkit.Testing.Aws on NuGet.org") | LocalStack SNS/SQS helpers (`SqsHelper`, `AwsMessageBrokerHelper`) and `SqsMessageFixture`. | Testing, Infrastructure.Messaging.Aws |
| [`Testing.Web`](Testing.Web/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.Web "Anarchy.Toolkit.Testing.Web on NuGet.org") | `RestApiTestCaseBase<TStartup, TAppSettings>` (native and Lambda pipelines), `TestableLambdaEntrypoint` and `MockAuthorizationToken<TAppSettings>(scopes)`. | Testing, Web |
| [`Testing.MockServer`](Testing.MockServer/README.md) [📦](https://www.nuget.org/packages/Anarchy.Toolkit.Testing.MockServer "Anarchy.Toolkit.Testing.MockServer on NuGet.org") | `MockServerClient` for the mock server's control API, received-request assertions (`AssertReceivedAsync`, `ShouldHaveReceived`), `RecordedRequestsFile` and `MockServerFixture`, which connects to `MOCKSERVER_URL` or spawns a `MockServerProcess`. | Testing, Web.MockServer |
