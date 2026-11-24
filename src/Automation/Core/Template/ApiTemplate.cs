namespace Anarchy.Toolkit.Automation.Core.Template;

/// <summary>
/// A REST API on <c>RestApiWebAppStartupBase</c> with JWT authorization and one anonymous endpoint.
/// </summary>
internal static class ApiTemplate
{
    /// <summary>
    /// The project files, keyed by path relative to the project directory.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Render(ApplicationContext context)
    {
        var app = context.App;
        var ns = context.Namespace;
        var scope = ApplicationTemplates.Kebab(app.Name);
        var themeUsing = app.SwaggerTheme ? string.Empty : "\nusing Anarchy.Toolkit.Web.Entity.Swagger;";
        var disableTheme = app.SwaggerTheme
            ? string.Empty
            : "\n            .Configure<SwaggerThemeOptions>(theme => theme.Enabled = false)";
        var files = new Dictionary<string, string> {
            [$"{context.ProjectName}.csproj"] = ApplicationTemplates.Project("Microsoft.NET.Sdk.Web", context, exe: true),
            [$"Composition/{context.StartupName}.cs"] = $$"""
                using {{ns}}.Composition.DependencyInjection;
                using {{ns}}.Infrastructure.Entity.Configuration;
                using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
                using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
                using Anarchy.Toolkit.Core.Health.Checks;
                using Anarchy.Toolkit.Web.Composition.Startup;{{themeUsing}}
                using Anarchy.Toolkit.Web.Extension.DependencyInjection;

                namespace {{ns}}.Composition;

                public sealed class {{context.StartupName}}(IWebHostEnvironment environment, IConfiguration configuration)
                    : RestApiWebAppStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
                {
                    protected override string ApiName => "{{app.Name}} API";

                    protected override IServiceCollection ConfigureWebCustomServices(IServiceCollection services) =>
                        base.ConfigureWebCustomServices(services)
                            .UseModule(new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(), Configuration, Environment)
                            .SetupAuthorization(readScope: "{{scope}}:read", writeScope: "{{scope}}:write", adminScope: "{{scope}}:admin")
                            .AddSingleton<IReadOnlyCollection<IHealthCheck>>([]){{disableTheme}};
                }

                """,
            ["Composition/DependencyInjection/ModuleAppSettings.cs"] = $$"""
                using {{ns}}.Infrastructure.Entity.Configuration;
                using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Entity;
                using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;

                namespace {{ns}}.Composition.DependencyInjection;

                public sealed class ModuleAppSettings : ModuleAppSettingsBase<AppSettings>
                {
                    protected override IServiceCollection RegisterAdditionalTypesForSettings(IServiceCollection services) =>
                        services.AddSingleton<IAuthServiceAppSettings>(sp => sp.GetRequiredService<AppSettings>());
                }

                """,
            ["Infrastructure/Entity/Configuration/AppSettings.cs"] = $$"""
                using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
                using System.ComponentModel.DataAnnotations;

                namespace {{ns}}.Infrastructure.Entity.Configuration;

                public sealed class AppSettings : IAuthServiceAppSettings
                {
                    [Required] public AuthServiceSettings AuthService { get; init; } = default!;
                }

                """,
            ["Presentation/Controller/PingController.cs"] = $$"""
                using Anarchy.Toolkit.Core.Telemetry;
                using Microsoft.AspNetCore.Authorization;
                using Microsoft.AspNetCore.Mvc;

                namespace {{ns}}.Presentation.Controller;

                [Route("ping")]
                [Produces("application/json")]
                public sealed class PingController(AppTelemetry telemetry) : ControllerBase
                {
                    [HttpGet]
                    [AllowAnonymous]
                    public IActionResult Ping()
                    {
                        telemetry.LogInformation(this.CalledFrom(), "Ping");
                        return Ok(new { message = "pong" });
                    }
                }

                """,
            ["appsettings.json"] = $$"""
                {
                  "AuthService": {
                    "Host": "https://auth.example.local/",
                    "ClientId": "{{scope}}-api",
                    "ClientSecretName": "{{scope}}-api-client-secret"
                  }
                }

                """
        };
        foreach (var (path, content) in WebEntryPointTemplates.Render(context)) files.Add(path, content);
        return files;
    }
}
