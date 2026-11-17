# Building UIs

`Anarchy.Toolkit.Web.Ui` provides a generic UI host: an interactive-server Blazor application behind a cookie session, where users sign in with OAuth client credentials and the UI calls downstream APIs on their behalf. Everything domain-specific (APIs, pages and environment URLs) is supplied by the application. The examples below build an administration UI for a user-management API.

[`samples/Samples.Ui`](https://github.com/anarchy-io/anarchy-toolkit/tree/main/samples/Samples.Ui) is a runnable example of the features described in this guide.

## What you get

| Piece | Where | What it does |
|---|---|---|
| `UiWebAppStartupBase<TAppSettingsDiModule, TAppSettings>` | `Ui/Composition/Startup` | Razor components, interactive server mode, cascading auth state, presenters, cookie session, login/logout endpoints, static assets. |
| `UiAuthenticationOptions` | `Entity/Ui` | Cookie settings, login/logout/home paths, OAuth `Scope`, and the `Environments` a session can target. |
| `UiRoutingOptions` | `Entity/Ui` | Route prefixes and the optional `/{environment}` link prefix, for hosts served behind an API gateway stage. |
| `UiEnvironment` / `IUiEnvironmentCatalog` | `Core/Entity/Ui`, `Core/Service/Ui` | Auth service URL and named downstream API URLs per environment. |
| `IUiSessionContext` | `Core/Service/Ui/Session` | Access token, client name and target environment of the signed-in user. |
| `UiApiClientBase` + `AddToolkitUiApiHttpClient` | `Infrastructure/Service/Http/Client`, `Configuration/Extension/Microsoft/DependencyInjection` | Bearer-authenticated JSON/GraphQL calls to the session's environment; failures raise `DownstreamHttpRequestException` with a readable body. |
| `NotificationService`, `JsonModalState`, `PaginationState` | `Ui/Presentation` | Circuit-scoped notifications and JSON modal, client-side pagination. |
| Components | `Ui/Template/Component` | `LoginForm`, `RedirectToLogin`, `RootRedirect`, `LogoutRedirect`, `SessionBadge`, `NotificationHost`, `JsonModalHost`, `JsonViewLink`, `JsonObjectProperties`, `ConfirmationModal`, `TypedConfirmationModal`, `Pagination`, `TablePaginationFooter`, `EnvironmentPill`, `UiHeadAssets`, `UiBodyScripts`, `LoginLayout`. |
| `toolkit-ui.css`, `toolkit-ui.js` | `wwwroot` (embedded) | Styles for the components above, layered on the consumer's theme stylesheet; scroll-edge shadows for wide tables. |

## Wiring a UI

```csharp
public class UserAdminStartup(IWebHostEnvironment environment, IConfiguration configuration)
    : UiWebAppStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
{
    protected override string ApiName => "User Admin";

    protected override UiAuthenticationOptions GetUiAuthenticationOptions() => new() {
        Scope = "UserApi:Admin",
        Environments = [
            new UiEnvironment("Development", new Uri("http://localhost:6001/"),
                new Dictionary<string, Uri> { ["UserApi"] = new("https://localhost:5005") }),
            new UiEnvironment("Testing", new Uri("https://auth.testing.example.com/"),
                new Dictionary<string, Uri> { ["UserApi"] = new("https://users.testing.example.com/") }),
        ]
    };

    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services)
    {
        base.ConfigureWebCustomServices(services)
            .AddToolkitUiApiHttpClient(TimeSpan.FromSeconds(30));

        return services.AddScoped<IUserApiClient, UserApiClient>();
    }

    protected override void ConfigureUiEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }
}
```

The login (`POST /endpoint/auth/login`, antiforgery-protected) and logout (`GET /endpoint/auth/logout`) endpoints are mapped for you. Locally, the login form lets the user select any catalog environment; deployed hosts always use their own environment, so it must be in the catalog.

A downstream client only names the API and the route:

```csharp
public class UserApiClient(IHttpClientFactory factory, IUiSessionContext session, IUiEnvironmentCatalog catalog, Observer observer)
    : UiApiClientBase(factory, session, catalog, observer), IUserApiClient
{
    public async Task<UserOutput> GetUserAsync(string id) =>
        await SendAsync<UserOutput>(HttpMethod.Get, await GetApiBaseUriAsync("UserApi"), BindRoute("users/{id}", ("id", id)));
}
```

### Static web assets outside Development

Blazor's `_framework/blazor.web.js`, and the static assets of referenced packages, come from the build's static web
assets manifest. ASP.NET Core only loads it in `Development`, so an unpublished build run in `Local`, `Testing` or a test
host would answer an empty script and render blank interactive pages. `UseToolkitHostUi` calls
`UseToolkitUiStaticWebAssets()` first to load it in every environment; call it yourself before `MapStaticAssets()` if you
build the pipeline without `UseToolkitHostUi`. Published applications are unaffected: their assets are in `wwwroot`.

## Pages and layout

Pages belong to the application (routes are compile-time constants), composed from the toolkit components. Add `@using Anarchy.Toolkit.Web.Ui.Ui.Template.Component.Session` (and `.Shared`, `.Json`, `.Layout`) to the application's `_Imports.razor`, and register the toolkit assembly with the router only if you add routable components of your own there.

```razor
@* App.razor *@
<head>
    <UiHeadAssets Stylesheets='["user-admin.css"]' />
</head>
<body>
    <Routes />
    <UiBodyScripts />
</body>

@* Routes.razor: <NotAuthorized><RedirectToLogin /></NotAuthorized> *@

@* Pages/Login.razor *@
@page "/ui/login"
@layout LoginLayout
<LoginForm Title="User Admin" LogoPath="logo.png" Subtitle="User management" />

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
<div class="users-table table-with-footer">
    <div class="table-scroll">
        <table class="sticky-edges-table">...</table>
    </div>
    <TablePaginationFooter State="Presenter.Pagination" OnChanged="Presenter.Refresh" />
</div>
<Pagination State="Presenter.Pagination" OnChanged="Presenter.Refresh" />
```

In presenters, report failures with `notifications.Error(exception)`; a `DownstreamHttpRequestException` shows the downstream response body under "More details".
