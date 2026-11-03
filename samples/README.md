# Samples

Applications built on the toolkit, one per kind of workload a typical service runs. They use only the toolkit's own defaults and extension points. None of these projects is packed, and the layering check ignores them.

| Project | Kind | Shows |
|---|---|---|
| [`Samples.Api`](Samples.Api) | REST API | `RestApiWebAppStartupBase`, a controller using `Observer`, Kestrel locally (`Program.cs`) and AWS Lambda behind API Gateway (`LambdaEntryPoint`), which opts into the Parameter Store through its `ConfigureAppConfiguration` hook. |
| [`Samples.Job`](Samples.Job) | AWS Lambda job | `JobStartupBase` + `JobEntryPointBase`, a scoped service wrapped in `MonitoredActionAsync`. |
| [`Samples.Worker`](Samples.Worker) | EC2/systemd worker | `WorkerEntryPointBase`, `HeartbeatWorker` and a custom `MonitoredWorkerBase`. |
| [`Samples.Ui`](Samples.Ui) | Back-office UI | `UiWebAppStartupBase` with two environments, an orders page built from the `Web.Ui` components (`LoginForm`, `SessionBadge`, `NotificationHost`, `JsonModalHost`/`JsonViewLink`, `Pagination`, `TablePaginationFooter`, `ConfirmationModal`, `TypedConfirmationModal`), and a `UiApiClientBase` client. Stub auth and orders endpoints under `/stub` (local hosts only) let it run without external services. |
| [`Samples.Tests`](Samples.Tests) | Tests | Smoke tests running the API through both the native and Lambda pipelines (`RestApiTestCaseBase`), invoking the job handler, and exercising the UI's shell and stubs. |

Every sample registers `ModuleLibConfiguration`, so it gets the toolkit's vendor-neutral `ObserverFactory`. To use an organisation's own sinks, log properties or metrics backend, pass a derived factory through `new ModuleLibConfiguration<…>(new ModuleObservability(new MyObserverFactory()))`. See the [observability guide](../docs/guides/observability.md#plugging-in-an-organisations-setup).

Run the API locally:

```bash
dotnet run --project samples/Samples.Api --environment Development
```

Run the UI locally, then open https://localhost:5101 and log in with client ID `samples-ui` and secret `samples-ui-secret` (the stub's test credentials, overridable through `StubClientId`/`StubClientSecret`):

```bash
dotnet run --project samples/Samples.Ui --environment Development
```

Pick either environment on the login page: each has its own orders. Cancelling a shipped order shows how a failed downstream call is reported; priority orders (★) ask for typed confirmation.
