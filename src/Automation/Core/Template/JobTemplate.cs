using Anarchy.Toolkit.Automation.Core.Entity;

namespace Anarchy.Toolkit.Automation.Core.Template;

/// <summary>A locally runnable job with an optional scheduled Lambda handler.</summary>
internal static class JobTemplate
{
    public static IReadOnlyDictionary<string, string> Render(ApplicationContext context)
    {
        var ns = context.Namespace;
        var startup = context.StartupName;
        var worker = context.App.JobMode == JobExecutionMode.Worker;
        var files = new Dictionary<string, string> {
            [$"{context.ProjectName}.csproj"] = ApplicationTemplates.Project("Microsoft.NET.Sdk", context, exe: true),
            ["EntryPointLocal.cs"] = worker ? $$"""
                using {{ns}}.Composition;
                using Anarchy.Toolkit.Runtime.Host;
                using Microsoft.Extensions.Hosting;

                namespace {{ns}};

                public static class EntryPointLocal
                {
                    public static async Task Main(string[] args)
                    {
                        await using var worker = new EntryPointWorker();
                        await worker.App.RunAsync();
                    }
                }

                public sealed class EntryPointWorker : WorkerEntryPointBase<{{startup}}>;

                """ : $$"""
                using {{ns}}.Composition;
                using {{ns}}.Core.Service;
                using Anarchy.Toolkit.Runtime.Host;
                using Microsoft.Extensions.DependencyInjection;

                namespace {{ns}};

                public static class EntryPointLocal
                {
                    public static async Task Main(string[] args)
                    {
                        await using var job = new LocalJob();
                        Console.WriteLine(await job.RunAsync(args.FirstOrDefault() ?? "local"));
                    }
                }

                public sealed class LocalJob : JobEntryPointBase<{{startup}}>
                {
                    public async Task<string> RunAsync(string input)
                    {
                        await using var scope = App.Services.CreateAsyncScope();
                        return await scope.ServiceProvider.GetRequiredService<JobService>().RunAsync(input);
                    }
                }

                """,
            [$"Composition/{startup}.cs"] = $$"""
                using {{context.ModuleNamespace}};
                using {{ns}}.Core.Service;
                using {{context.SettingsNamespace}};
                using Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration;
                using Anarchy.Toolkit.Core.Extensions.Microsoft.Extensions.DependencyInjection;
                using Anarchy.Toolkit.Runtime.Host;
                using Microsoft.Extensions.Configuration;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace {{ns}}.Composition;

                public sealed class {{startup}}(IHostEnvironment environment, IConfiguration configuration)
                    : JobStartupBase<ModuleAppSettings, AppSettings>(environment, configuration)
                {
                    protected override IServiceCollection ConfigureJobCustomServices(IServiceCollection services) =>
                        services
                            .UseModule(new ModuleLibConfiguration<ModuleAppSettings, AppSettings>(), Configuration, Environment)
                            .AddScoped<JobService>(){{(worker ? "\n            .AddHostedService<JobWorker>()" : "")}};
                }

                """,
            ["Composition/DependencyInjection/ModuleAppSettings.cs"] = $$"""
                using {{ns}}.Infrastructure.Entity.Configuration;
                using Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration.Entity;

                namespace {{ns}}.Composition.DependencyInjection;

                public sealed class ModuleAppSettings : ModuleAppSettingsBase<AppSettings>;

                """,
            ["Infrastructure/Entity/Configuration/AppSettings.cs"] = $$"""
                using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;

                namespace {{ns}}.Infrastructure.Entity.Configuration;

                public sealed class AppSettings : IAppSettings;

                """,
            ["Core/Service/JobService.cs"] = $$"""
                using Anarchy.Toolkit.Core.Telemetry;
                using Anarchy.Toolkit.Core.Telemetry.Extensions.System;

                namespace {{ns}}.Core.Service;

                public sealed class JobService(AppTelemetry telemetry)
                {
                    public Task<string> RunAsync(string input) =>
                        telemetry.MonitoredActionAsync(this.CalledFrom(), () => {
                            telemetry.LogInformation(this.CalledFrom(), "Processing {Input}", input);
                            return Task.FromResult($"Processed {input}");
                        });
                }

                """
        };
        if (worker)
            files["Core/Service/JobWorker.cs"] = $$"""
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;

                namespace {{ns}}.Core.Service;

                public sealed class JobWorker(IServiceScopeFactory scopes) : BackgroundService
                {
                    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
                    {
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            await using (var scope = scopes.CreateAsyncScope())
                                await scope.ServiceProvider.GetRequiredService<JobService>().RunAsync("worker");
                            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                        }
                    }
                }

                """;
        if (context.App.EventLambda)
            files["EntryPointLambda.cs"] = $$"""
                using {{ns}}.Composition;
                using {{ns}}.Core.Service;
                using Anarchy.Toolkit.Runtime.Aws.Host;
                using Microsoft.Extensions.DependencyInjection;

                namespace {{ns}};

                /// <summary>Scheduled Lambda handler: <c>{{ns}}::{{ns}}.EntryPointLambda::HandleAsync</c>.</summary>
                public sealed class EntryPointLambda : LambdaJobEntryPointBase<{{startup}}>
                {
                    public async Task HandleAsync()
                    {
                        await using var scope = BeginInvocation();
                        await scope.ServiceProvider.GetRequiredService<JobService>().RunAsync("scheduled");
                    }
                }

                """;
        return files;
    }
}
