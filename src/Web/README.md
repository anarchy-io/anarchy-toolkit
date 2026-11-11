# Anarchy.Toolkit.Web

Shared ASP.NET Core REST API pipeline and presentation helpers.

```bash
dotnet add package Anarchy.Toolkit.Web
```

- Build an API using `RestApiWebAppStartupBase` and the web entry-point bases for native hosting or AWS Lambda.
- Compose the supplied middleware, authentication, Swagger, JSON converters and validation attributes around your application's endpoints.
- A health controller exposes the shared health-report model. Application-specific scopes and endpoints remain in the consumer.

## When to use it

Use this package to host a toolkit-based REST API on ASP.NET Core or AWS Lambda while sharing startup, authentication, health and error-handling conventions. It supplies infrastructure around your endpoints; it does not define your application's API contracts or business logic.

## How to use it

1. Define application settings and a settings module, then derive a startup from `RestApiWebAppStartupBase<TAppSettingsDiModule, TAppSettings>`. Register your services and configure authorization scopes in its service-configuration hook.
2. Implement controllers with explicit request/response types and call application services rather than database repositories directly. Keep transport DTOs separate from domain entities.
3. Supply the `IReadOnlyCollection<IServiceHealthReport>` consumed by the health controller, using an empty collection if there are no checks or registering the relevant provider reports.
4. Use `WebAppEntryPoint.RunWebHost<Startup>(args)` for a native host and an `AwsWebAppEntryPointBase` implementation for Lambda. Configure real authentication, secrets and environment-specific settings before deployment.

The [sample API startup](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Api/Composition/Startup.cs), [native entry point](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Api/Program.cs) and [Lambda entry point](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Api/Web/LambdaEntryPoint.cs) show the complete structure. Test the HTTP pipeline with `Testing.Web`.

See the [endpoint guide](https://github.com/anarchy-io/anarchy-toolkit/blob/main/docs/guides/exposing-endpoints.md), [API sample](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Api) and [toolkit overview](https://github.com/anarchy-io/anarchy-toolkit#readme).

## Releases

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced API hosting, controller and middleware infrastructure, authentication, Swagger and Lambda hosting helpers.
