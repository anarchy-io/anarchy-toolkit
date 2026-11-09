# Anarchy.Toolkit.Configuration

Application settings, secrets and observability bootstrap for toolkit-based services.

```bash
dotnet add package Anarchy.Toolkit.Configuration
```

- Implement the settings capability interfaces your application needs, then bind and expose them with a `ModuleAppSettingsBase<TAppSettings>` subclass.
- Use `ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings>` to register settings, observability and secrets together; customize observability with an `ObserverFactory`.
- Choose configuration sources in the application, including AWS Systems Manager Parameter Store via `AddAwsSystemsManagerParameterStore` when needed. Secret services include KMS, cached and in-memory implementations.

## When to use it

Use this package at the composition boundary of a service that needs typed settings, secret access and a consistent `Observer`. Application code owns its settings type and the sources used to populate it; the package does not prescribe a configuration file or automatically provision AWS resources.

## How to use it

1. Define an application settings type implementing `IAppSettings` and whichever capability interfaces the application consumes, such as `IAuthServiceAppSettings` or `IPostgresAppSettings`.
2. Derive a settings module from `ModuleAppSettingsBase<TAppSettings>`. Override `RegisterAdditionalTypesForSettings` to expose the same settings instance through the capability interfaces your services request.
3. Register `ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings>` using `UseModule`, passing the host configuration and environment. Supply a customized `ModuleObservability`/`ObserverFactory` if the defaults are not appropriate.
4. Choose configuration sources and secret providers in the host. Keep credentials outside committed configuration and grant the AWS permissions needed by any selected remote provider.

The [sample API settings module](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Api/Composition/DependencyInjection/ModuleAppSettings.cs) and [startup](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Api/Composition/Startup.cs) show the wiring end to end.

See the [observability guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/observability.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced application settings, configuration and secret management, observability setup and the AppRunner host helper.
