# Dependency Injection

How services are registered: one `IDependencyInjectionModule` per concern, applied by the composition root with `UseModule`. The [Layered Architecture guide](../guides/architecture.md#dependency-injection-modules) lists the toolkit's own modules; the `anarchy-compose` agent skill walks through writing one.

> **Recommendation, not a requirement.** The packages work without following this convention. It is a rule only for code in the toolkit repository (see [Contributing](../contributing/README.md)); applications can adopt it, adapt it or skip it. [Opting out or adapting](#opting-out-or-adapting) explains how.
{: .notice--info}

## Why

- **The application owns its configuration.** Modules take their options from the caller, so the same package works with any section name, settings type or secret store. See [ADR 0002](../decisions/0002-dependency-injection-modules.md) and the [Pluggable value](../decisions/README.md#values).
- **Composable and testable.** A module is a unit you can register on an empty `ServiceCollection` in a test and resolve in isolation.
- **Settings fail early.** `ModuleAppSettingsBase<TAppSettings>` validates the settings with [DataAnnotations](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations) on start, instead of on the first request that reads them. See [ADR 0003](../decisions/0003-validated-app-settings.md).

## The rules

Modules follow two rules:


1. **No hard-coded configuration.** A module never assumes a section name or a settings type. Options come in through constructor parameters, usually as a `Func<IConfiguration, TOptions>` evaluated when the module registers its services:

   ```csharp
   services.UseModule(
       new ModulePostgres(
           config => config.GetSection("Postgres").Get<PostgresConnectionOptions>()!,
           config => new PostgresResilienceOptions(config.GetValue<int>("Resilience:RetryCount"))),
       configuration,
       environment);
   ```

   Parameterless modules use `services.UseModule<TModule>(configuration, environment)`.

   The composition root passes the read as a lambda: `new ModuleCarrierClient(c => c.GetRequired<CarrierClientSettings>("CarrierClient"))`, or from the validated `AppSettings` (`c => c.Get<AppSettings>()!.CarrierClient`). The section name lives in the one place that knows the configuration, and a missing section fails at startup.
2. **Modules wrap extensions.** Where a project ships an `Add*` extension (e.g. `AddSnsMessageBroker`), the module calls it, so the same registrations are also available without a module.

Modules live under `DependencyInjection/Module/` and are named `Module<Concern>` (`ModulePostgres`, `ModuleCarrierClient`). `Core.*` projects ship plain [`IServiceCollection`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.iservicecollection) extensions instead (e.g. `AddLocalEventBus()`), since they have no configuration to receive.

## Opting out or adapting

| You want to... | Do this |
|---|---|
| Keep your `IServiceCollection` extensions | Keep them. Every toolkit module wraps an `Add*` extension or plain registrations you can call directly, so modules are never the only entry point. |
| Adopt modules step by step | Write one module per adapter (rule 2) and apply it with `services.UseModule(...)` from your `Startup`; see the [recipe](../guides/onboarding-recipes.md#write-a-module). |
| Keep [`IOptions<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options.ioptions-1) | Keep it. `ModuleAppSettingsBase` is optional; you can pass `c => c.GetSection("X").Get<T>()!` to toolkit modules from any settings model. |
| Use another container ([Autofac](https://autofac.org/), ...) | Modules only touch `IServiceCollection`, which other containers populate from. |

**What you lose by opting out:** start-up validation of all settings in one place, and the one-line wiring of `ModuleLibConfiguration` (settings, telemetry and secrets together).
