# Anarchy.Toolkit.Configuration

Application settings, secrets and observability bootstrap for toolkit-based services.

```bash
dotnet add package Anarchy.Toolkit.Configuration
```

- Implement the settings capability interfaces your application needs, then bind and expose them with a `ModuleAppSettingsBase<TAppSettings>` subclass.
- Use `ModuleLibConfiguration<TAppSettingsDiModule, TAppSettings>` to register settings, observability and secrets together; customize observability with an `ObserverFactory`.
- Choose configuration sources in the application, including AWS Systems Manager Parameter Store via `AddAwsSystemsManagerParameterStore` when needed. Secret services include KMS, cached and in-memory implementations.

See the [observability guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/observability.md) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).
