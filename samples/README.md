# Samples

Applications built on the [Anarchy I/O Toolkit platform](https://anarchy-io.github.io/anarchy-toolkit/overview/), one per kind of workload a typical service runs. They use only the toolkit's own defaults and extension points. None of these projects is packed, and the layering check ignores them.

No single sample covers the whole platform; together they show its three pillars composed:

- **Architecture**: each application registers its services through DI modules (`ModuleLibConfiguration`) and follows the [async](../docs/guides/async-conventions.md), [endpoint](../docs/guides/exposing-endpoints.md) and [testing](../docs/guides/testing-conventions.md) conventions.
- **Infrastructure**: each references only the packages its workload needs (below), plus their dependencies.
- **Build Ecosystem**: they are part of the solution, so this repository's Fallout build compiles and lints them with the packages they consume.

| Project | Packages referenced directly | Capabilities shown |
|---|---|---|
| `Samples.Api` | `Web` | REST pipeline, configuration, observability, Lambda hosting |
| `Samples.Job` | `Hosting` | Job hosting, configuration, observability |
| `Samples.Worker` | `Hosting`, `Hosting.Workers` | Worker hosting, heartbeat and monitored workers |
| `Samples.Ui` | `Web.Ui` | Blazor UI, cookie-session login, downstream API clients |
| `Samples.MockServer` | `Web.MockServer` | HTTP mock server with expectations and middlewares |
| `Samples.Tests` | `Testing.Web`, `Testing.MockServer` | API, job and UI smoke tests, mock-server assertions |

Persistence and messaging packages are not used by the samples; see the [Package Catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/) and [Database Providers](../docs/guides/database-providers.md).

| Project | Kind | Shows |
|---|---|---|
| [`Samples.Api`](Samples.Api) | REST API | `RestApiWebAppStartupBase`, a controller using `Observer`, Kestrel locally (`Program.cs`) and AWS Lambda behind API Gateway (`LambdaEntryPoint`), which opts into the Parameter Store through its `ConfigureAppConfiguration` hook. |
| [`Samples.Job`](Samples.Job) | AWS Lambda job | `JobStartupBase` + `JobEntryPointBase`, a scoped service wrapped in `MonitoredActionAsync`. |
| [`Samples.Worker`](Samples.Worker) | EC2/systemd worker | `WorkerEntryPointBase`, `HeartbeatWorker` and a custom `MonitoredWorkerBase`. |
| [`Samples.Ui`](Samples.Ui) | WebApp | `UiWebAppStartupBase` with two environments, an orders page built from the `Web.Ui` components (`LoginForm`, `SessionBadge`, `NotificationHost`, `JsonModalHost`/`JsonViewLink`, `Pagination`, `TablePaginationFooter`, `ConfirmationModal`, `TypedConfirmationModal`), and a `UiApiClientBase` client. Stub auth and orders endpoints under `/stub` (local hosts only) let it run without external services. |
| [`Samples.MockServer`](Samples.MockServer) | Mock server host | `MockServerEntryPoint` with JSON expectations for a payments service (`mocks/`), a conventional middleware rejecting requests without a bearer token, and an inline one computing stock levels from the request when no expectation matches. |
| [`Samples.Tests`](Samples.Tests) | Tests | Smoke tests running the API through both the native and Lambda pipelines (`RestApiTestCaseBase`), invoking the job handler, and exercising the UI's shell and stubs. Asserting what an application sent to the payments and stock services stood in by `Samples.MockServer` (`MockServerFixture`, `AssertReceivedAsync`). |

Every sample registers `ModuleLibConfiguration`, so it gets the toolkit's vendor-neutral `ObserverFactory`. To use an organization's own sinks, log properties or metrics backend, pass a derived factory through `new ModuleLibConfiguration<…>(new ModuleObservability(new MyObserverFactory()))`. See the [observability guide](../docs/guides/observability.md#plugging-in-an-organizations-setup).

Run the API locally:

```bash
dotnet run --project samples/Samples.Api --environment Development
```

Run the UI locally, then open https://localhost:5101 and log in with client ID `samples-ui` and secret `samples-ui-secret` (the stub's test credentials, overridable through `StubClientId`/`StubClientSecret`):

```bash
dotnet run --project samples/Samples.Ui --environment Development
```

Pick either environment on the login page: each has its own orders. Canceling a shipped order shows how a failed downstream call is reported; priority orders (★) ask for typed confirmation.

Run the mock server locally, showing request headers and bodies, then try `curl -H 'Authorization: Bearer x' -d '{"currency":"EUR"}' localhost:6001/payments` or `curl localhost:6001/stock/ABC-1`:

```bash
dotnet run --project samples/Samples.MockServer -- --verbosity detailed --record artifacts/mock-requests.jsonl
```

The `MockServerTests` in `Samples.Tests` spawn it on a free port, or use the one at `MOCKSERVER_URL` when it is set.
