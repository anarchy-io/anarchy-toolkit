# Anarchy.Toolkit.Runtime.Web.Swagger

> Part of [Anarchy I/O Toolkit](https://anarchy-io.github.io/anarchy-toolkit/overview/), a .NET developer platform · **Pillar:** [Infrastructure](https://anarchy-io.github.io/anarchy-toolkit/overview/#the-three-pillars) · **Layer:** Runtime · [Package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/)

Swagger/OpenAPI for toolkit REST APIs: the OpenAPI document and the themed Swagger UI.

```bash
dotnet add package Anarchy.Toolkit.Runtime.Web.Swagger
```

- `AddToolkitSwagger(apiName, appSettings, environment, ...)` registers the OpenAPI 3.2 document at `swagger/spec/swagger.json` and serves Swagger UI at `/swagger`, with `GET /` redirecting to it.
- The document declares an `oauth2` client-credentials scheme when the settings implement `IAuthServiceAppSettings`, documents `[Authorize]` policies (`AuthorizeCheckOperationFilter`) and the `Accept-Encoding` header (`AcceptEncodingHeaderOperationFilter`), describes the runtime (`SwaggerDescriptionBuilder`, `SwaggerDescriptionOptions`) and adds a server from `BASE_PATH`.
- Hosts composing their own pipeline call `AddToolkitSwaggerGen(...)` and `app.UseToolkitSwagger(...)` instead.

## When to use it

Use it for REST APIs that publish an OpenAPI document or a Swagger UI. APIs without them only need `Anarchy.Toolkit.Runtime.Web`, which references neither Swashbuckle nor Microsoft.OpenApi. `anarchy-toolkit scaffold api` adds it unless `--no-swagger` is given.

## How to use it

```csharp
public sealed class Startup(IWebHostEnvironment environment, IConfiguration configuration)
    : RestApiWebAppStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
{
    protected override string ApiName => "Orders API";

    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services) =>
        base.ConfigureWebCustomServices(services)
            .AddToolkitSwagger(ApiName, AppSettings, Environment, titlePrefix: "[Orders]");
}
```

`oauthScopes` lists the scopes offered by the Swagger UI client-credentials flow, `titlePrefix` (default `[API]`) prefixes the document and page titles, and `descriptionOptions` adds repository, dashboard, documentation and team links to the description.

### Swagger UI theme

Swagger UI is themed by default, with the Anarchy palette, header logo (dark text in light mode, white text in dark mode) and favicon, in light and dark mode. Swagger UI starts in dark mode when the system prefers it; the choice made with its header toggle is remembered in the browser. The theme is a mechanism rather than a brand: the stylesheet reads every colour, font and radius from a separate colours stylesheet, so you can rebrand by changing only the colours.

- **Change the colours.** Copy [`wwwroot/css/swagger-colors.css`](https://github.com/anarchy-io/anarchy-toolkit/blob/main/src/Runtime.Web.Swagger/wwwroot/css/swagger-colors.css) to your API project's `wwwroot/css/swagger-colors.css` and edit the values. Each colour is a `light-dark(<light>, <dark>)` pair. The packaged assets follow the usual `wwwroot` layout (`css/swagger-colors.css`, `css/swagger-theme.css` and `js/swagger-theme.js` in this package; `images/logo.png`, `images/logo-dark.png`, and `favicon.svg` and `favicon.png` at the root in `Anarchy.Toolkit.Runtime.Web`), and files in your API project's `wwwroot` are served first, so a `wwwroot/favicon.svg` or `wwwroot/images/logo.png` replaces the packaged one the same way.
- **Point elsewhere.** `services.Configure<SwaggerThemeOptions>(...)` sets `ColorsStylesheet`, `Stylesheet`, `Script`, `Logo`, `DarkLogo`, `Favicon` and `FallbackFavicon` (relative to the application base path, or absolute). Set `Stylesheet` to your own file to replace the theme entirely.
- **Turn it off.** `services.Configure<SwaggerThemeOptions>(theme => theme.Enabled = false)` keeps Swagger UI's own look and favicon.

The theme uses system fonts only and changes colours, type and borders, not layouts. Swagger UI's class names are not a stable API, so check the theme after upgrading Swashbuckle.

The OpenAPI document is served as OpenAPI 3.2 by default; set `SwaggerOptions.OpenApiVersion` with `services.Configure<SwaggerOptions>(...)` to serve another version.

See the [package catalog](https://anarchy-io.github.io/anarchy-toolkit/packages/) and [toolkit overview](https://anarchy-io.github.io/anarchy-toolkit/overview/).

## Third-party dependencies

This package brings these libraries, which its name does not announce:

| Library | Why | In the public API? |
|---|---|---|
| [Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore) | Generates the OpenAPI document and serves Swagger UI | Yes: the operation filters implement its `IOperationFilter`; `SwaggerOptions` sets the OpenAPI version |
| [Microsoft.OpenApi](https://www.nuget.org/packages/Microsoft.OpenApi) | The OpenAPI document model | Yes: the filters take `OpenApiOperation` |

## Releases

See the [release notes](RELEASE-NOTES.md) for the complete release history.
