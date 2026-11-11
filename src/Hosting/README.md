# Anarchy.Toolkit.Hosting

Generic startup and entry-point infrastructure for jobs and worker processes.

```bash
dotnet add package Anarchy.Toolkit.Hosting
```

- Implement `IStartup` and use `UseStartup<TStartup>(appRunnerType)` to compose a host.
- Build jobs and workers with `JobStartupBase`, `JobEntryPointBase`, `WorkerEntryPointBase` and `AppHostBuilder`.
- Override `ConfigureAppConfiguration` to select the application's configuration sources; `DiFactory<TAppSettingsDiModule, TAppSettings>` builds the service container.

## When to use it

Use Hosting for the composition and lifecycle of a job or worker that is not itself an HTTP API. It separates application startup and service registration from the executable entry point, allowing the same conventions to be used in different hosting environments.

## How to use it

1. Define your settings and settings module using `Anarchy.Toolkit.Configuration`.
2. Implement an `IStartup` or derive from the relevant startup base. Register application services and any toolkit modules in the startup's service-configuration hook.
3. Create an entry point using the appropriate job or worker base; run the host and dispose it when the process exits. Select configuration sources in `ConfigureAppConfiguration` when the defaults are insufficient.

See the runnable [worker startup](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Worker/Composition/WorkerStartup.cs), [worker entry point](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Worker/Composition/WorkerEntryPoint.cs), and [Lambda job](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Job) for concrete implementations. For REST endpoints, use `Anarchy.Toolkit.Web`; for long-running worker and health primitives, add `Hosting.Workers`.

See the [sample applications](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced the startup contract, host builder, dependency-injection factory and job and worker entry-point bases.
