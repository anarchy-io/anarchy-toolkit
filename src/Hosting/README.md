# Anarchy.Toolkit.Hosting

Generic startup and entry-point infrastructure for jobs and worker processes.

```bash
dotnet add package Anarchy.Toolkit.Hosting
```

- Implement `IStartup` and use `UseStartup<TStartup>(appRunnerType)` to compose a host.
- Build jobs and workers with `JobStartupBase`, `JobEntryPointBase`, `WorkerEntryPointBase` and `AppHostBuilder`.
- Override `ConfigureAppConfiguration` to select the application's configuration sources; `DiFactory<TAppSettingsDiModule, TAppSettings>` builds the service container.

See the [sample applications](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
