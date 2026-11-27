# Anarchy.Toolkit.Web

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Web · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Shared ASP.NET Core REST API pipeline and presentation helpers.

```bash
dotnet add package Anarchy.Toolkit.Web
```

- Build an API using `RestApiWebAppStartupBase` and the web entry-point bases for native or container hosting; AWS Lambda hosting lives in `Anarchy.Toolkit.Runtime.Web.Aws`.
- Compose the supplied middleware, authentication, JSON converters and validation attributes around your application's endpoints.
- A health controller exposes the shared health-report model. Application-specific scopes and endpoints remain in the consumer.
- Swagger is opt-in: add `Anarchy.Toolkit.Runtime.Web.Swagger` and call `AddToolkitSwagger(ApiName, AppSettings, Environment)` from the startup. Without it, this package references neither Swashbuckle nor Microsoft.OpenApi.

## When to use it

Use this package to host a toolkit-based REST API on ASP.NET Core or AWS Lambda while sharing startup, authentication, health and error-handling conventions. It supplies infrastructure around your endpoints; it does not define your application's API contracts or business logic.

## How to use it

1. Define application settings and a settings module, then derive a startup from `RestApiWebAppStartupBase<TAppSettingsDiModule, TAppSettings>`. Register your services and configure authorization scopes in its service-configuration hook.
2. Implement controllers with explicit request/response types and call application services rather than database repositories directly. Keep transport DTOs separate from domain entities.
3. Supply the `IReadOnlyCollection<IHealthCheck>` consumed by the health controller, using an empty collection if there are no checks or registering the relevant provider reports.
4. Use `WebAppEntryPoint.RunWebHost<Startup>(args)` for a native host and an `AwsWebAppEntryPointBase` implementation (from `Anarchy.Toolkit.Runtime.Web.Aws`) for Lambda. Configure real authentication, secrets and environment-specific settings before deployment.

The [sample API startup](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Api/Composition/SampleApiStartup.cs), [native entry point](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Api/EntryPointLocal.cs) and [Lambda entry point](https://github.com/anarchy-io/anarchy-toolkit/blob/main/samples/Samples.Api/EntryPointLambda.cs) show the complete structure. Test the HTTP pipeline with `Testing.Web`.

See the [endpoint guide](https://anarchy-io.github.io/anarchy-toolkit/conventions/endpoints.html), [API sample](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Api) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
