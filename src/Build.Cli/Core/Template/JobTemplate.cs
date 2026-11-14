namespace Anarchy.Toolkit.Build.Cli.Core.Template;

/// <summary>
/// A job on <c>JobEntryPointBase</c> (e.g. an AWS Lambda handler) delegating to one scoped service.
/// </summary>
internal static class JobTemplate
{
    /// <summary>
    /// The project files, keyed by path relative to the project directory.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Render(ApplicationContext context)
    {
        var ns = context.Namespace;
        return new Dictionary<string, string> {
            [$"{context.ProjectName}.csproj"] = ApplicationTemplates.Project("Microsoft.NET.Sdk", context, exe: false),
            ["Function.cs"] = $$"""
                using {{ns}}.Composition;
                using {{ns}}.Core.Service;
                using Anarchy.Toolkit.Hosting.Host;
                using Microsoft.Extensions.DependencyInjection;

                namespace {{ns}};

                /// <summary>
                /// Job handler (<c>{{ns}}::{{ns}}.Function::HandleAsync</c> as an AWS Lambda function).
                /// </summary>
                public sealed class Function : JobEntryPointBase<JobStartup>
                {
                    public async Task<string> HandleAsync(string input)
                    {
                        using var scope = App.Services.CreateScope();

                        return await scope.ServiceProvider.GetRequiredService<JobService>().RunAsync(input);
                    }
                }

                """,
            ["Composition/JobStartup.cs"] = $$"""
                using {{ns}}.Composition.DependencyInjection;
                using {{ns}}.Core.Service;
                using {{ns}}.Infrastructure.Entity.Configuration;
                using Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration;
                using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
                using Anarchy.Toolkit.Hosting.Host;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace {{ns}}.Composition;

                public sealed class JobStartup(IHostEnvironment environment, IConfiguration configuration)
                    : JobStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
                {
                    protected override IServiceCollection ConfigureJobCustomServices(IServiceCollection services) =>
                        services
                            .UseModule(new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(), Configuration, Environment)
                            .AddScoped<JobService>();
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

                namespace {{ns}}.Infrastructure.Entity.Configuration;

                /// <summary>
                /// The job's settings, supplied by the environment.
                /// </summary>
                public sealed class AppSettings : IAppSettings;

                """,
            ["Core/Service/JobService.cs"] = $$"""
                using Anarchy.Toolkit.Core.Observability.Extension;
                using Anarchy.Toolkit.Core.Observability.Service.Observability;

                namespace {{ns}}.Core.Service;

                public sealed class JobService(Observer observer)
                {
                    public Task<string> RunAsync(string input) =>
                        observer.MonitoredActionAsync(this.CalledFrom(), () => {
                            observer.LogInformation(this.CalledFrom(), "Processing {Input}", input);
                            return Task.FromResult($"Processed {input}");
                        });
                }

                """
        };
    }
}
