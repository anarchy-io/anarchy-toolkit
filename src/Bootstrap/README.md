# Anarchy.Toolkit.Bootstrap

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Bootstrap · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Application settings, secrets and provider-neutral telemetry bootstrap for toolkit-based services.

```bash
dotnet add package Anarchy.Toolkit.Bootstrap
```

- Implement the settings capability interfaces your application needs, then bind and expose them with a `ModuleAppSettingsBase<TAppSettings>` subclass.
- Use `ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings>` to register settings, telemetry, correlation context and secrets together.
- `ModuleTelemetry` registers the toolkit telemetry (`AppTelemetry`, `ICoreTelemetry`, `IMetricRecorder`), the correlation context and `IApplicationContext`. It adds no sink: logs go to the host's logging providers, and registrations the host made are kept. Pass a `ModuleTelemetryBase` (`new ModuleTelemetry(new CompanyTelemetryModule())`) to register the company's logging and metrics as a module.
- Choose configuration sources in the application, such as the AWS Systems Manager Parameter Store from `Anarchy.Toolkit.Bootstrap.Aws`. Secret services include cached and in-memory implementations; remote ones (e.g. AWS KMS) are opted into with `ModuleLibConfiguration.WithSecretManagement`.

## When to use it

Use this package at the composition boundary of a service that needs typed settings, secret access and the shared `AppTelemetry`. Application code owns its settings type, logging providers and configuration sources; the package does not prescribe a configuration file or automatically provision AWS resources.

## How to use it

1. Define an application settings type implementing `IAppSettings` and whichever capability interfaces the application consumes, such as `IAuthServiceAppSettings` or `IPostgresAppSettings`.
2. Derive a settings module from `ModuleAppSettingsBase<TAppSettings>`. Override `RegisterAdditionalTypesForSettings` to expose the same settings instance through the capability interfaces your services request.
3. Register `ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings>` using `UseModule`. Configure logging providers, a metric recorder or an `ICoreTelemetry` in the host, or pass them as a `ModuleTelemetryBase` through `new ModuleLibConfiguration<…>(new ModuleTelemetry(…))`.
4. Choose configuration sources and secret providers in the host. Keep credentials outside committed configuration. For AWS sources and KMS secrets, add `Anarchy.Toolkit.Bootstrap.Aws` and call `.WithSecretManagement(new ModuleAwsSecretManagement())` on the `ModuleLibConfiguration`, granting the AWS permissions it needs.

The [sample API settings module](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Api/Composition/DependencyInjection/ModuleAppSettings.cs) and [startup](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Api/Composition/SampleApiStartup.cs) show the wiring end to end.

See the [observability guide](https://anarchy-io.github.io/anarchy-toolkit/guides/observability.html) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
