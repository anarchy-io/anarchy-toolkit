---
name: anarchy-compose
description: "Composing an Anarchy Toolkit application at runtime: writing an IDependencyInjectionModule and applying it with UseModule, validated settings with ModuleAppSettingsBase, health reports on HealthCheckBase, background workers on MonitoredWorkerBase, registering telemetry (AppTelemetry, OpenTelemetry/OTLP, optional Serilog, correlation scopes) and plugging in a backend, and testing a module in isolation. Use it when adding DI registrations, settings, health checks, workers or telemetry."
---

# Runtime composition

How an application built on the toolkit wires itself up. The package READMEs (in the NuGet cache,
`~/.nuget/packages/anarchy.toolkit.<package>/<version>/README.md`), the architecture guide
(https://anarchy-io.github.io/anarchy-toolkit/guides/architecture/) and the `anarchy-conventions` skill
(dependency injection, observability) are the reference; this skill is the recipe.

## When to use

- Registering services for a feature or an adapter in a module.
- Adding settings that must be validated when the application starts.
- Adding a health check for a dependency, or a background worker.
- Instrumenting code with logs and metrics, or choosing where they are sent.

## Modules

One `IDependencyInjectionModule` (`Anarchy.Toolkit.Core.Composition.Module`) per feature or adapter, in
`DependencyInjection/Module/Module<Name>.cs` of the lowest layer that holds everything it registers (usually
`Bootstrap`):

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
(`using Anarchy.Toolkit.Core;`), or `UseModule<T>` for a
parameterless module. Rules:

1. **No hard-coded configuration.** A module never reads a section name or assumes the settings type: options come in
   through the constructor, usually as a `Func<IConfiguration, TOptions>`, so the composition root owns where they
   live. This keeps modules reusable and testable with any configuration.
2. **Modules wrap extensions.** If an `Add*` extension exists, the module calls it, so the same registrations are also
   available without a module.

**Where options come from.** The composition root passes the read as a lambda, for example
`new ModuleCarrierClient(c => c.Get<AppSettings>()!.CarrierClient)` from the validated `AppSettings`, or
`c => c.GetRequired<CarrierClientSettings>("CarrierClient")` straight from configuration. The section name lives in
one place, and a missing section fails at startup.

## Settings

One `AppSettings` class implementing `IAppSettings` (`Anarchy.Toolkit.Bootstrap`), with DataAnnotations on its
properties, and a `ModuleAppSettings : ModuleAppSettingsBase<AppSettings>`. It binds the root configuration, validates
on start (`ValidateDataAnnotations`, `ValidateOnStart`), and registers `AppSettings`, `IOptions<AppSettings>` and
`IAppSettings`; override `RegisterAdditionalTypesForSettings` to expose capability interfaces (`IAwsAppSettings`,
`IPostgresAppSettings`, ...). `ValidateDataAnnotations` does not recurse into nested settings objects: validate them
from `AppSettings` (implement `IValidatableObject` and run `Validator.TryValidateObject` on each). `ModuleLibConfiguration<ModuleAppSettings, AppSettings>` registers the settings module, telemetry
and secrets in one go; hosts build the container with `DiFactory<ModuleAppSettings, AppSettings>`.

## Health reports

One class per dependency, deriving `HealthCheckBase` (`Anarchy.Toolkit.Core.Health`): set `Name` (meaningful
to whoever supports the service) and implement `CheckAsync` (return to pass, throw to fail; timing and failures are
handled). Register them as `IHealthCheck`; persistence packages ship their own (`ModulePostgresHealthReport`).
Workers serve them with `AddServiceHealthCheckWorker(assembly)` (`/health/instance`, `/health/services`,
`/health/status`).

## Workers

Derive `MonitoredWorkerBase(telemetry, recurringIntervalInSeconds, delayedIntervalInSeconds)`
(`Anarchy.Toolkit.Runtime.Workers`) and implement `RunIterationAsync`: return `true` when it did work (next run after
the recurring interval) or `false` when idle (after the delayed one). Start, stop, failures and a `health.ping` metric
per iteration are recorded; an exception ends the loop and is logged as fatal without stopping the host. Register it
with `services.AddHostedService<TWorker>()` in a module.

## Telemetry

Everything logs and measures through `AppTelemetry` (`Anarchy.Toolkit.Core.Telemetry`), which sits on the standard
Microsoft `ILogger` and .NET metrics. The application's providers and exporters decide where telemetry goes.

**Register.** `ModuleLibConfiguration` registers it through `ModuleTelemetry`; elsewhere call `services.AddCoreTelemetry()`.
It uses `TryAdd`: what the application registered first is kept, anything registered later wins, and the toolkit never
disposes instances it was given. To register the application's logging and metrics once, derive a module:

```csharp
public sealed class AcmeTelemetryModule : ModuleTelemetryBase
{
    protected override void RegisterCoreTelemetry(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddLogging(logging => logging.AddJsonConsole(o => o.IncludeScopes = true));
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddMeter(CoreTelemetry.InstrumentationName).AddOtlpExporter())
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation().AddOtlpExporter());
    }
}
// new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(new ModuleTelemetry(new AcmeTelemetryModule()))
```

**Backends.** OTLP is the preferred path: the exporter reads `OTEL_EXPORTER_OTLP_ENDPOINT`, `_HEADERS`, `_PROTOCOL` and
`OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE`. Datadog and New Relic ingest OTLP, so they are configuration only,
never a vendor API in code. Serilog is optional: `services.AddSerilogTelemetry(config => config.WriteTo...)` from
`Infrastructure.Telemetry.Serilog` adds it as a logging provider without changing instrumentation code. A custom
`IMetricRecorder` registered in DI replaces or decorates the default only when the standard meter is not enough.

**Instrument.** Inject `AppTelemetry`, take the call site once (`var caller = this.CalledFrom();`) and log or count
through it: `telemetry.LogInformation(caller, "...{OrderId}", id)`,
`telemetry.IncrementCounter(caller, MetricSuffixes.ExecutionCount, 1, new("success", true))`,
`telemetry.MonitoredActionAsync(caller, ...)`. Notable occurrences are a structured log with a stable `EventId` (and a
counter if the count matters); there is no vendor event API. Keep IDs in log properties, never in metric tags.

**Correlation.** `UseCorrelationId()` handles HTTP. Work outside a request opens a scope:
`using (CorrelationContext.BeginScope(logger, correlationId, attemptId))`, and for a mapped SQS message
`using (message.BeginCorrelationScope(logger))`. The AWS package ships no consumer loop, so the handler must call it.
The IDs travel as `ILogger` scope properties and activity tags (enable `IncludeScopes` on the console or OpenTelemetry
logger). Serilog's `Enrich.WithCorrelationId()` is only for events outside such a scope.

**Tests.** `TestCaseBase.FixtureTelemetry.Telemetry` is an `AppTelemetry` capturing logs and metrics in memory.

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
