using Anarchy.Toolkit.Build.Cli.Core.Entity;

namespace Anarchy.Toolkit.Build.Cli.Core.Template;

/// <summary>
/// An interactive-server Blazor UI on <c>UiWebAppStartupBase</c>: cookie-session login against the configured auth
/// service, and an authorized home page.
/// </summary>
internal static class UiTemplate
{
    public static IReadOnlyDictionary<string, string> Render(ApplicationContext context)
    {
        var app = context.App;
        var ns = context.Namespace;
        var ports = ApplicationTemplates.PortsOf(context);
        var title = $"{app.Name} UI";
        return new Dictionary<string, string> {
            [$"{context.ProjectName}.csproj"] = ApplicationTemplates.Project("Microsoft.NET.Sdk.Web", context, exe: true),
            ["Program.cs"] = $"""
                using {ns}.Composition;
                using Anarchy.Toolkit.Web;

                // Local development: Kestrel on http://localhost:{ports.Http} and https://localhost:{ports.Https}.
                WebAppEntryPoint.RunWebHost<Startup>(args, {ports.Http}, {ports.Https});

                """,
            ["Composition/Startup.cs"] = $$"""
                using {{ns}}.Composition.DependencyInjection;
                using {{ns}}.Infrastructure.Entity.Configuration;
                using {{ns}}.Ui.Template;
                using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
                using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
                using Anarchy.Toolkit.Web.Ui.Core.Entity.Ui;
                using Anarchy.Toolkit.Web.Ui.Entity.Ui;
                using Anarchy.Toolkit.Web.Ui.Ui.Composition.Startup;

                namespace {{ns}}.Composition;

                public sealed class Startup(IWebHostEnvironment environment, IConfiguration configuration)
                    : UiWebAppStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
                {
                    protected override string ApiName => "{{title}}";

                    /// <summary>
                    /// Sessions target the host's own environment. List further environments, with the downstream API URLs
                    /// of each, to let local users pick one at login.
                    /// </summary>
                    protected override UiAuthenticationOptions GetUiAuthenticationOptions()
                    {
                        var settings = Configuration.Get<AppSettings>() ?? new AppSettings();

                        return new UiAuthenticationOptions {
                            Scope = settings.Scope,
                            Environments = [
                                new UiEnvironment(Environment.EnvironmentName, settings.AuthServiceBaseUrl, new Dictionary<string, Uri>())
                            ]
                        };
                    }

                    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services) =>
                        base.ConfigureWebCustomServices(services)
                            .UseModule(new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(), Configuration, Environment);

                    protected override void ConfigureUiEndpoints(IEndpointRouteBuilder endpoints) =>
                        endpoints
                            .MapRazorComponents<App>()
                            .AddInteractiveServerRenderMode();
                }

                """,
            ["Composition/DependencyInjection/ModuleAppSettings.cs"] = $"""
                using {ns}.Infrastructure.Entity.Configuration;
                using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Entity;

                namespace {ns}.Composition.DependencyInjection;

                public sealed class ModuleAppSettings : ModuleAppSettingsBase<AppSettings>;

                """,
            ["Infrastructure/Entity/Configuration/AppSettings.cs"] = $$"""
                using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
                using System.ComponentModel.DataAnnotations;

                namespace {{ns}}.Infrastructure.Entity.Configuration;

                public sealed class AppSettings : IAppSettings
                {
                    /// <summary>The OAuth service users sign in against with their client credentials.</summary>
                    [Required] public Uri AuthServiceBaseUrl { get; init; } = new("https://auth.example.local/");

                    /// <summary>The scope requested at login; empty grants every scope allowed for the client.</summary>
                    public string? Scope { get; init; }
                }

                """,
            ["Ui/Template/_Imports.razor"] = $"""
                @using global::{ns}.Ui.Template.Component.Layout
                @using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Presenter
                @using Anarchy.Toolkit.Web.Ui.Ui.Template.Component.Json
                @using Anarchy.Toolkit.Web.Ui.Ui.Template.Component.Layout
                @using Anarchy.Toolkit.Web.Ui.Ui.Template.Component.Session
                @using Anarchy.Toolkit.Web.Ui.Ui.Template.Component.Shared
                @using Microsoft.AspNetCore.Authorization
                @using Microsoft.AspNetCore.Components
                @using Microsoft.AspNetCore.Components.Authorization
                @using Microsoft.AspNetCore.Components.Routing
                @using Microsoft.AspNetCore.Components.Web

                """,
            ["Ui/Template/App.razor"] = """
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="utf-8" />
                    <UiHeadAssets />
                    <HeadOutlet @rendermode="@(new InteractiveServerRenderMode(prerender: false))" />
                </head>
                <body>
                    <Routes />
                    <UiBodyScripts />
                </body>
                </html>

                """,
            ["Ui/Template/Routes.razor"] = $$"""
                @rendermode @(new InteractiveServerRenderMode(prerender: false))

                <Router AppAssembly="@typeof(App).Assembly">
                    <Found Context="routeData">
                        <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                            <NotAuthorized>
                                <RedirectToLogin />
                            </NotAuthorized>
                        </AuthorizeRouteView>
                        <FocusOnNavigate RouteData="@routeData" Selector="main h1" />
                    </Found>
                    <NotFound>
                        <PageTitle>{{title}} / Not found</PageTitle>
                        <MainLayout>
                            <h1>Page not found</h1>
                            <p>Sorry, there's nothing at this address.</p>
                        </MainLayout>
                    </NotFound>
                </Router>

                """,
            ["Ui/Template/Component/Layout/MainLayout.razor"] = $$"""
                @inherits LayoutComponentBase

                @inject BasePresenter Presenter

                <div class="layout">
                    <header class="navbar-shell">
                        <nav class="navbar">
                            <div class="navbar-content">
                                <div class="navbar-brand">
                                    <h1><a href="@Presenter.GetUiRoute()">{{title}}</a></h1>
                                </div>
                                <ul class="navbar-nav">
                                    <li><SessionBadge /></li>
                                </ul>
                            </div>
                        </nav>

                        <NotificationHost />
                    </header>

                    <main class="main-content">
                        @Body
                    </main>

                    <JsonModalHost />
                </div>

                """,
            ["Ui/Template/Component/Pages/Root.razor"] = """
                @page "/"

                <RootRedirect />

                """,
            ["Ui/Template/Component/Pages/Login.razor"] = $$"""
                @page "/ui/login"
                @layout LoginLayout

                <PageTitle>{{title}} / Login</PageTitle>

                <LoginForm Title="{{title}}" />

                """,
            ["Ui/Template/Component/Pages/Logout.razor"] = """
                @page "/ui/logout"

                <LogoutRedirect />

                """,
            ["Ui/Template/Component/Pages/Home.razor"] = $$"""
                @page "/ui"
                @attribute [Authorize]

                <PageTitle>{{title}}</PageTitle>

                <h1>{{title}}</h1>
                <p>Signed in. Add your pages under <code>Ui/Template/Component/Pages</code>.</p>

                """
        };
    }
}
