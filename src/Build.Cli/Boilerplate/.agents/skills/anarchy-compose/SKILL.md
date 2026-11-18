---
name: anarchy-compose
description: "Composing an Anarchy Toolkit application at runtime: writing an IDependencyInjectionModule and applying it with UseModule, validated settings with ModuleAppSettingsBase, health reports on ServiceHealthReportBase, background workers on MonitoredWorkerBase, logging and metrics through Observer, and testing a module in isolation. Use it when adding or converting DI registrations, settings, health checks or workers."
---

# Runtime composition

How an application built on the toolkit wires itself up. The package READMEs (in the NuGet cache,
`~/.nuget/packages/anarchy.toolkit.<package>/<version>/README.md`) and the architecture guide
(https://anarchy-io.github.io/anarchy-toolkit/guides/architecture/) are the reference; this skill is the recipe.

## When to use

- Registering services for a feature or an adapter, or converting `Add*`/`Register*` extensions to modules.
- Adding settings that must be validated when the application starts.
- Adding a health check for a dependency, or a background worker.
- Instrumenting code with logs and metrics.

## Modules

One `IDependencyInjectionModule` (`Anarchy.Toolkit.Core.Composition.Module`) per feature or adapter, in
`DependencyInjection/Module/Module<Name>.cs` of the lowest layer that holds everything it registers (usually
`Configuration`):

```csharp
public sealed class ModuleCarrierClient(Func<IConfiguration, CarrierClientSettings> settings) : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        var options = settings(configuration);
        services.AddSingleton(options);
        services.AddHttpClient<ICarrierClient, CarrierClient>(client => client.BaseAddress = options.BaseAddress);
        return services;
    }
}
```

The composition root applies it: `services.UseModule(new ModuleCarrierClient(...), configuration, environment)`
(`using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;`), or `UseModule<T>` for a
parameterless module. Rules:

1. **No hard-coded configuration.** A module never reads a section name or assumes the settings type: options come in
   through the constructor, usually as a `Func<IConfiguration, TOptions>`, so the composition root owns where they
   live. This keeps modules reusable and testable with any configuration.
2. **Modules wrap, they don't replace.** If an `Add*` extension exists, the module calls it, so callers that do not use
   modules keep working.

**Converting existing code.** Code that reads `configuration.GetRequired<T>("Section")` inside an `Add*` extension
keeps the read, but moves it to the caller: the extension becomes a module taking `Func<IConfiguration, T>`, and the
composition root passes the old read as a lambda, `new ModuleCarrierClient(c => c.GetRequired<CarrierClientSettings>("CarrierClient"))`.
Behaviour stays the same (it still fails on a missing section), the section name now lives in one place, and the
module can later take settings from the validated `AppSettings` instead (`c => c.Get<AppSettings>()!.CarrierClient`).
Convert one extension per change and keep the old extension delegating to the module until no caller is left.

## Settings

One `AppSettings` class implementing `IAppSettings` (`Anarchy.Toolkit.Configuration`), with DataAnnotations on its
properties, and a `ModuleAppSettings : ModuleAppSettingsBase<AppSettings>`. It binds the root configuration, validates
on start (`ValidateDataAnnotations`, `ValidateOnStart`), and registers `AppSettings`, `IOptions<AppSettings>` and
`IAppSettings`; override `RegisterAdditionalTypesForSettings` to expose capability interfaces (`IAwsAppSettings`,
`IPostgresAppSettings`, ...). `ValidateDataAnnotations` does not recurse into nested settings objects: validate them
from `AppSettings` (implement `IValidatableObject` and run `Validator.TryValidateObject` on each). `ModuleLibConfiguration<ModuleAppSettings, AppSettings>` registers the settings module, observability
and secrets in one go; hosts build the container with `DiFactory<ModuleAppSettings, AppSettings>`.

## Health reports

One class per dependency, deriving `ServiceHealthReportBase` (`Anarchy.Toolkit.Core.Health`): set `Name` (meaningful
to whoever supports the service) and implement `CheckAsync` (return to pass, throw to fail; timing and failures are
handled). Register them as `IServiceHealthReport`; persistence packages ship their own (`ModulePostgresHealthReport`).
Workers serve them with `AddServiceHealthCheckWorker(assembly)` (`/health/instance`, `/health/services`,
`/health/status`).

## Workers

Derive `MonitoredWorkerBase(observer, recurringIntervalInSeconds, delayedIntervalInSeconds)`
(`Anarchy.Toolkit.Hosting.Workers`) and implement `RunIterationAsync`: return `true` when it did work (next run after
the recurring interval) or `false` when idle (after the delayed one). Start, stop, failures and a `health.ping` metric
per iteration are recorded; an exception ends the loop and is logged as fatal without stopping the host. Register it
with `services.AddHostedService<TWorker>()` in a module.

## Observability

Inject `Observer` (`Anarchy.Toolkit.Core.Observability`), take the call site once (`var caller = this.CalledFrom();`)
and log or count through it (`observer.LogInformation(caller, "...{OrderId}", id)`,
`observer.ExecutionIncrementCount(caller, 1, Tags.Success)`, `observer.MonitoredActionAsync(...)`). Keep IDs in log
properties, never in metric tags. Converting another monitoring facade is a separate change from converting DI: do
not mix them.

## Testing a module in isolation

```csharp
[Fact]
public void ModuleCarrierClient_RegistersTheClient()
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["CarrierClient:BaseAddress"] = "http://localhost:6001" })
        .Build();
    var services = new ServiceCollection()
        .UseModule(new ModuleCarrierClient(c => c.GetSection("CarrierClient").Get<CarrierClientSettings>()!),
                   configuration, Mock.Of<IHostEnvironment>());

    using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    provider.GetRequiredService<ICarrierClient>().Should().NotBeNull();
}
```

`ValidateOnBuild` catches missing registrations; for settings, also resolve `IOptions<AppSettings>.Value` with
invalid values and expect an `OptionsValidationException`.

## Rules

- Put a module in the lowest layer that can hold its registrations; `make check_layering` must pass.
- One concern per change: moving registrations into modules must not change behaviour or the contract.
- Before finishing: `make compile test_unit lint check_layering`.
