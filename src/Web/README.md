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

See the [endpoint guide](https://anarchy-io.github.io/anarchy-toolkit/guides/exposing-endpoints.html), [API sample](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Api) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Swagger UI theme

Swagger UI is themed by default, with the Anarchy palette, header logo (dark text in light mode, white text in dark mode) and favicon, in light and dark mode. Swagger UI starts in dark mode when the system prefers it; the choice made with its header toggle is remembered in the browser. The theme is a mechanism rather than a brand: the stylesheet reads every colour, font and radius from a separate colours stylesheet, so you can rebrand by changing only the colours.

- **Change the colours.** Copy [`wwwroot/css/swagger-colors.css`](https://github.com/anarchy-io/anarchy-toolkit/blob/main/src/Web/wwwroot/css/swagger-colors.css) to your API project's `wwwroot/css/swagger-colors.css` and edit the values. Each colour is a `light-dark(<light>, <dark>)` pair. The packaged assets follow the usual `wwwroot` layout (`css/swagger-colors.css`, `css/swagger-theme.css`, `js/swagger-theme.js`, `images/logo.png`, `images/logo-dark.png`, and `favicon.svg` and `favicon.png` at the root), and files in your API project's `wwwroot` are served first, so a `wwwroot/favicon.svg` or `wwwroot/images/logo.png` replaces the packaged one the same way.
- **Point elsewhere.** `services.Configure<SwaggerThemeOptions>(...)` sets `ColorsStylesheet`, `Stylesheet`, `Script`, `Logo`, `DarkLogo`, `Favicon` and `FallbackFavicon` (relative to the application base path, or absolute). Set `Stylesheet` to your own file to replace the theme entirely.
- **Turn it off.** `services.Configure<SwaggerThemeOptions>(theme => theme.Enabled = false)` keeps Swagger UI's own look and favicon.

The theme uses system fonts only and changes colours, type and borders, not layouts. Swagger UI's class names are not a stable API, so check the theme after upgrading Swashbuckle.

The OpenAPI document is served as OpenAPI 3.2 by default; set `SwaggerOptions.OpenApiVersion` with `services.Configure<SwaggerOptions>(...)` to serve another version.

## Releases

### 1.2.0

- build(deps): upgrade Swashbuckle.AspNetCore to 11.0.1 and Microsoft.OpenApi to 3.11.1
- feat(Web): serve OpenAPI 3.2 and theme Swagger UI with a light and dark Anarchy theme

### 1.0.8

- docs: expand XML docblocks across all projects for the DocFX API reference

### 1.0.7

- `Microsoft.OpenApi` updated to 2.13.0 and `System.Security.Cryptography.*` aligned with the .NET 10 LTS (10.0.15, down from 11.0.0).

### 1.0.6

- refactor(src): clean up code style across library packages
- chore(deps): update AWS SDK, Lambda ASP.NET Core server and MySqlConnector packages

### 1.0.5

- chore(deps): update AWS SDK packages

### 1.0.3

- build: migrate local automation to build tooling

### 1.0.2

- docs: expand package guides and release histories
- chore(deps): update AWS, Polly, and xUnit packages

### 1.0.1

- Added a package-specific NuGet README.

### 1.0.0

- Introduced API hosting, controller and middleware infrastructure, authentication, Swagger and Lambda hosting helpers.
