# Building UIs

`Anarchy.Toolkit.Web.Ui` is a generic back-office UI: an interactive-server Blazor app behind a cookie session, where developers log in with OAuth client credentials and the UI calls downstream APIs on their behalf. Everything domain-specific (which APIs, which pages, which environment URLs) is supplied by the consumer.

[`samples/Samples.Ui`](../../samples/Samples.Ui) is a runnable example of everything below.

## What you get

| Piece | Where | What it does |
|---|---|---|
| `UiWebAppStartupBase<TAppSettingsDiModule, TAppSettings>` | `Ui/Composition/Startup` | Razor components, interactive server mode, cascading auth state, presenters, cookie session, login/logout endpoints, static assets. |
| `UiAuthenticationOptions` | `Entity/Ui` | Cookie settings, login/logout/home paths, OAuth `Scope`, and the `Environments` a session can target. |
| `UiRoutingOptions` | `Entity/Ui` | Route prefixes and the optional `/{environment}` link prefix on TAP hosts. |
| `UiEnvironment` / `IUiEnvironmentCatalog` | `Core/Entity/Ui`, `Core/Service/Ui` | Auth service URL and named downstream API URLs per environment. |
| `IUiSessionContext` | `Core/Service/Ui/Session` | Access token, client name and target environment of the signed-in user. |
| `UiApiClientBase` + `AddToolkitUiApiHttpClient` | `Infrastructure/Service/Http/Client`, `Configuration/Extension/Microsoft/DependencyInjection` | Bearer-authenticated JSON/GraphQL calls to the session's environment; failures raise `DownstreamHttpRequestException` with a readable body. |
| `NotificationService`, `JsonModalState`, `PaginationState` | `Ui/Presentation` | Circuit-scoped notifications and JSON modal, client-side pagination. |
| Components | `Ui/Template/Component` | `LoginForm`, `RedirectToLogin`, `RootRedirect`, `LogoutRedirect`, `SessionBadge`, `NotificationHost`, `JsonModalHost`, `JsonViewLink`, `JsonObjectProperties`, `ConfirmationModal`, `TypedConfirmationModal`, `Pagination`, `TablePaginationFooter`, `EnvironmentPill`, `UiHeadAssets`, `UiBodyScripts`, `LoginLayout`. |
| `toolkit-ui.css`, `toolkit-ui.js` | `wwwroot` (embedded) | Styles for the components above, layered on the consumer's theme stylesheet; scroll-edge shadows for wide tables. |

## Wiring a UI

```csharp
public class BackofficeStartup(IWebHostEnvironment environment, IConfiguration configuration)
    : UiWebAppStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
{
    protected override string ApiName => "My UI";

    protected override UiAuthenticationOptions GetUiAuthenticationOptions() => new() {
        Scope = "MyApi:Admin",
        Environments = [
            new UiEnvironment("Development", new Uri("http://localhost:6001/"),
                new Dictionary<string, Uri> { ["MyApi"] = new("https://localhost:5005") }),
            new UiEnvironment("Testing", new Uri("https://auth.testing.example.com/"),
                new Dictionary<string, Uri> { ["MyApi"] = new("https://myapi.testing.example.com/") }),
        ]
    };

    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        base.ConfigureWebCustomServices(services)
            .AddToolkitUiApiHttpClient(TimeSpan.FromSeconds(30));

        return services.AddScoped<IMyApiClient, MyApiClient>();
    }

    protected override void ConfigureUiEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }
}
```

The login (`POST /endpoint/auth/login`, antiforgery-protected) and logout (`GET /endpoint/auth/logout`) endpoints are mapped for you. Locally the login form lets the developer pick any catalog environment; deployed hosts always use their own environment, so it must be in the catalog.

A downstream client only names the API and the route:

```csharp
public class MyApiClient(IHttpClientFactory factory, IUiSessionContext session, IUiEnvironmentCatalog catalog, Observer observer)
    : UiApiClientBase(factory, session, catalog, observer), IMyApiClient
{
    public async Task<OrderOutput> GetOrderAsync(string id) =>
        await SendAsync<OrderOutput>(HttpMethod.Get, await GetApiBaseUriAsync("MyApi"), BindRoute("orders/{id}", ("id", id)));
}
```

## Pages and layout

Pages stay in the consumer (routes are compile-time constants), composed from the toolkit components. Add `@using Anarchy.Toolkit.Web.Ui.Ui.Template.Component.Session` (and `.Shared`, `.Json`, `.Layout`) to your `_Imports.razor`, and register the toolkit assembly with the router only if you add routable components of your own there.

```razor
@* App.razor *@
<head>
    <UiHeadAssets Stylesheets='["my-ui.css"]' />
</head>
<body>
    <Routes />
    <UiBodyScripts />
</body>

@* Routes.razor: <NotAuthorized><RedirectToLogin /></NotAuthorized> *@

@* Pages/Login.razor *@
@page "/ui/login"
@layout LoginLayout
<LoginForm Title="My UI" LogoPath="logo.png" Subtitle="Developer Admin Panel" />

@* Pages/Logout.razor *@
@page "/ui/logout"
<LogoutRedirect />

@* Layout/MainLayout.razor *@
<header class="navbar-shell">
    <nav class="navbar">... <SessionBadge /></nav>
    <NotificationHost />
</header>
<main class="main-content">@Body</main>
<JsonModalHost />
```

A paginated table pairs the toolkit's classes with `PaginationState` (call `Paginate` once per change and keep the result):

```razor
<div class="events-table table-with-footer">
    <div class="table-scroll">
        <table class="sticky-edges-table">...</table>
    </div>
    <TablePaginationFooter State="Presenter.Pagination" OnChanged="Presenter.Refresh" />
</div>
<Pagination State="Presenter.Pagination" OnChanged="Presenter.Refresh" />
```

In presenters, report failures with `notifications.Error(exception)`; a `DownstreamHttpRequestException` shows the downstream response body under "More details".
