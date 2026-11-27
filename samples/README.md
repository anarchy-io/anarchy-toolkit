# Samples

Applications built on the [Anarchy I/O Toolkit platform](https://anarchy-io.github.io/anarchy-toolkit/overview/), one per kind of workload a typical service runs. They use only the toolkit's own defaults and extension points. None of these projects is packed, and the layering check ignores them.

No single sample covers the whole platform; together they show its three pillars composed:

- **Architecture**: each application registers its services through DI modules (`ModuleLibConfiguration`) and follows the [async](../docs/portal/conventions/async.md), [endpoint](../docs/portal/conventions/endpoints.md) and [testing](../docs/portal/conventions/testing.md) conventions.
- **Infrastructure**: each references only the packages its workload needs (below), plus their dependencies.
- **Build Ecosystem**: they are part of the solution, so this repository's Fallout build compiles and lints them with the packages they consume.

| Project | Packages referenced directly | Capabilities shown |
|---|---|---|
| `Samples.Api` | `Runtime.Web.Aws`, `Runtime.Web.Swagger`, `Infrastructure.Persistence.Db.Postgres`, `Infrastructure.Persistence.Db.Mysql`, `Infrastructure.Persistence.Db.Oracle` | REST pipeline, configuration, telemetry, Lambda hosting, health reports for all three database providers |
| `Samples.Job` | `Runtime.Aws` | Job hosting, configuration, telemetry |
| `Samples.Worker` | `Runtime`, `Runtime.Workers`, `Bootstrap.Aws` | Worker hosting, heartbeat and monitored workers |
| `Samples.Ui` | `Runtime.Web.Ui` | Blazor UI, cookie-session login, downstream API clients |
| `Samples.MockServer` | `Runtime.Web.MockServer`, `Runtime.Web.MockServer.Ui` | HTTP mock server with expectations and middlewares |
| `Samples.Examples` | `Bootstrap`, `Core.Persistence.Abstractions`, `Infrastructure.Telemetry.Serilog` | Telemetry providers and exporters, a UseCase and an Aggregate |
| `Samples.Testing` | `Testing`, `Testing.Runtime.Web.Aws`, `Testing.MockServer` | Showcase of the Testing packages: in-memory telemetry, API, job and UI smoke tests, mock-server assertions |

The API's `/health/services` endpoint probes the local PostgreSQL, MySQL, and Oracle databases. The standard Integration
suite also exercises SNS/SQS against Moto; see the [local Compose guide](../docker/README.md), [Package Catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/), and [Database Providers](../docs/portal/guides/database-providers.md).

| Project | Kind | Shows |
|---|---|---|
| [`Samples.Api`](Samples.Api) | REST API | `RestApiWebAppStartupBase`, a controller using `AppTelemetry`, local Kestrel and container hosting through `EntryPointLocal`, and AWS Lambda behind API Gateway through `EntryPointLambda`, which opts into Parameter Store through its `ConfigureAppConfiguration` hook. `GET /health/services` reports the connection health of PostgreSQL, MySQL, and Oracle through the Toolkit's provider health reports. |
| [`Samples.Job`](Samples.Job) | Local and AWS Lambda job | `JobStartupBase` + `JobEntryPointBase`, a scoped service wrapped in `AppTelemetry.MonitoredActionAsync`; run locally once or invoke the scheduled Lambda handler. |
| [`Samples.Worker`](Samples.Worker) | EC2/systemd worker | `WorkerEntryPointBase`, `HeartbeatWorker` and a custom `MonitoredWorkerBase`. |
| [`Samples.Ui`](Samples.Ui) | WebApp | `UiWebAppStartupBase` with two environments, an orders page built from the `Runtime.Web.Ui` components (`LoginForm`, `SessionBadge`, `NotificationHost`, `JsonModalHost`/`JsonViewLink`, `Pagination`, `TablePaginationFooter`, `ConfirmationModal`, `TypedConfirmationModal`), and a `UiApiClientBase` client. Stub auth and orders endpoints under `/stub` (local hosts only) let it run without external services. |
| [`Samples.MockServer`](Samples.MockServer) | Mock server host | `MockServerEntryPoint` with JSON expectations for a payments service (`mocks/`), a conventional middleware rejecting requests without a bearer token, and an inline one computing stock levels from the request when no expectation matches. Serves the UI at `/__mock/ui`. |
| [`Samples.Examples`](Samples.Examples) | Console examples | Run one with `dotnet run --project samples/Samples.Examples -- <name>`. Telemetry: `default` (`AddCoreTelemetry()` with Microsoft logging and standard metrics), `serilog` (`AddSerilogTelemetry` with a consumer sink receiving the toolkit's events), `swap` (the same `CheckoutService` on Microsoft logging, then Serilog), `recorder` (a custom `IMetricRecorder`) and `otlp` (OpenTelemetry exporting the toolkit meter over OTLP; set `OTLP_ENDPOINT`). `orders` shows a `PlaceOrderUseCase` that owns the transaction and an `OrderAggregate` that joins it, over an in-memory `IUnitOfWork`. |
| [`Samples.Testing`](Samples.Testing) | Compiled Testing showcase | Smoke tests running the API through both the native and Lambda pipelines (`RestApiTestCaseBase`), invoking the job handler, and exercising the UI's shell and stubs. Asserting what an application sent to the payments and stock services stood in by `Samples.MockServer` (`MockServerFixture`, `AssertReceivedAsync`). |

The application samples register `ModuleLibConfiguration`; the standalone telemetry examples use `AddCoreTelemetry()` directly. Both get the toolkit's provider-neutral `ICoreTelemetry`, `IMetricRecorder` and `AppTelemetry`. Logs go to the logging providers the application registers and metrics use standard .NET metrics; to use an organization's own sinks or metrics backend, register them (or pass a `ModuleTelemetryBase` through `new ModuleLibConfiguration<…>(new ModuleTelemetry(new MyTelemetryModule()))`). See the [observability guide](../docs/portal/guides/observability.md).

Start the local dependencies, then run the API locally and query its database health endpoint:

```bash
make start
dotnet run --project samples/Samples.Api --environment Development
# In another terminal: curl http://localhost:5000/health/services
```

Run the UI locally, then open https://localhost:5101 and log in with client ID `anarchy` and secret `secret` (the stub's test credentials, overridable through `StubClientId`/`StubClientSecret`):

```bash
dotnet run --project samples/Samples.Ui --environment Development
```

Pick either environment on the login page: each has its own orders. Canceling a shipped order shows how a failed downstream call is reported; priority orders (★) ask for typed confirmation.

To run the UI's container entry point locally (Docker uses the same published application and startup):

```bash
dotnet publish samples/Samples.Ui -c Release -o artifacts/samples-ui
docker run --rm -p 3000:3000 -e RUNTIME_HOST=container -e ASPNETCORE_ENVIRONMENT=Development -e StubBaseUrl=http://127.0.0.1:3000/ -v "$PWD/artifacts/samples-ui:/app:ro" -w /app mcr.microsoft.com/dotnet/aspnet:10.0 dotnet Anarchy.Toolkit.Samples.Ui.dll
```

Open `http://localhost:3000`. Container hosts listen on all interfaces, use `PORT` (default 3000), and expose `/health/instance`. Forwarded headers are trusted from the upstream network; restrict access to trusted proxies. The API has the same container dispatch; its Lambda handler opts into application-owned Parameter Store configuration. `ASPNETCORE_ENVIRONMENT`/`DOTNET_ENVIRONMENT` select configuration separately from `RUNTIME_HOST`.

Run the one-shot job locally with `dotnet run --project samples/Samples.Job -- Ada`. `EntryPointScheduled::HandleAsync` is a no-input scheduled Lambda handler; it shares the local job's startup and service.

Run the mock server locally, showing request headers and bodies, then try `curl -H 'Authorization: Bearer x' -d '{"currency":"EUR"}' localhost:6001/payments` or `curl localhost:6001/stock/ABC-1`:

```bash
dotnet run --project samples/Samples.MockServer -- --verbosity detailed --record artifacts/mock-requests.jsonl
# then open http://localhost:6001/__mock/ui/ (or pass --no-ui)
```

`Samples.Testing` is compiled with the solution, so CI proves it builds against the Testing packages, but it is not part of the regular test run (`make test_unit`). Run it on demand with `cd samples/Samples.Testing && dotnet run -c Release`. Its `MockServerTests` spawn it on a free port, or use the one at `MOCKSERVER_URL` when it is set.
