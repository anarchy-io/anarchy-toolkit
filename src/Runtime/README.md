# Anarchy.Toolkit.Runtime

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Runtime · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Generic startup and entry-point infrastructure for jobs and worker processes.

```bash
dotnet add package Anarchy.Toolkit.Runtime
```

- Implement `IStartup` and use `UseStartup<TStartup>(appRunnerType)` to compose a host.
- Build jobs and workers with `JobStartupBase`, `JobEntryPointBase`, `WorkerEntryPointBase` and `AppHostBuilder`.
- Override `ConfigureAppConfiguration` to select the application's configuration sources; `DiFactory<TAppSettingsDiModule, TAppSettings>` builds the service container.

## When to use it

Use Runtime for the composition and lifecycle of a job or worker that is not itself an HTTP API. It separates application startup and service registration from the executable entry point, allowing the same conventions to be used in different hosting environments.

## How to use it

1. Define your settings and settings module using `Anarchy.Toolkit.Bootstrap`.
2. Implement an `IStartup` or derive from the relevant startup base. Register application services and any toolkit modules in the startup's service-configuration hook.
3. Create an entry point using the appropriate job or worker base; run the host and dispose it when the process exits. Select configuration sources in `ConfigureAppConfiguration` when the defaults are insufficient.

See the runnable [worker startup](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Worker/Composition/SampleWorkerStartup.cs), [worker entry point](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Worker/EntryPointWorker.cs), and [Lambda job](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Job) for concrete implementations. For REST endpoints, use `Anarchy.Toolkit.Runtime.Web`; for long-running worker and health primitives, add `Runtime.Workers`; for AWS Lambda jobs, add `Runtime.Aws`.

See the [sample applications](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [Microsoft.Extensions.Hosting.Systemd](https://www.nuget.org/packages/Microsoft.Extensions.Hosting.Systemd) | `AppHostBuilder` enables systemd integration (`UseSystemd`) | No |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
