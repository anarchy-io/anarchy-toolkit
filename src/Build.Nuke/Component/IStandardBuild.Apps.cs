using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Build.Nuke.Component
{
    // `Configuration` would otherwise resolve to this project's Configuration namespace (see the architecture guide).
    using Configuration = Anarchy.Toolkit.Build.Nuke.Core.ValueSet.Build.Configuration;

    public partial interface IStandardBuild
    {
        [Parameter("Runnable app name to execute (any src/** project with OutputType=Exe)")]
        string? AppName => TryGetValue(() => AppName);

        [Parameter("Runnable app names to execute in parallel (comma-separated)")]
        string? AppNames => TryGetValue(() => AppNames);

        Target RunApp => d => d
            .Description("Builds the solution and runs one runnable app from src/**")
            .Executes(() => {
                if (string.IsNullOrWhiteSpace(AppName))
                {
                    Log.Warning("No app specified. Pass --AppName <name> to run one of these apps:");
                    LogRunnableApps();
                    throw new Exception("Missing required --AppName parameter.");
                }

                var appProject = ResolveRunnableProjectOrFail(AppName);

                Log.Information("Running {AppName} from {RelativePath}", appProject.Name, RelativePath(appProject));

                DotNetRun(s => s
                    .SetProjectFile(appProject.Path)
                    .SetConfiguration(Configuration.Debug)
                    .SetVerbosity(DotNetVerbosity.minimal));
            });

        Target RunApps => d => d
            .Description("Builds the solution and runs multiple runnable apps from src/** in parallel")
            .Executes(() => {
                if (string.IsNullOrWhiteSpace(AppNames))
                {
                    Log.Warning("No apps specified. Pass --AppNames <name1,name2,...> to run multiple apps.");
                    LogRunnableApps();
                    throw new Exception("Missing required --AppNames parameter.");
                }

                var appNames = ParseAppNames(AppNames);

                if (appNames.Count == 0)
                {
                    throw new Exception("No valid app names were provided.");
                }

                var appProjects = ResolveRunnableProjectsOrFail(appNames);
                var processes = new List<IProcess>();
                var stopping = false;

                void StopAll()
                {
                    if (stopping) return;
                    stopping = true;

                    Log.Information("Stopping all running apps...");

                    foreach (var process in processes)
                    {
                        try
                        {
                            if (!process.HasExited)
                                process.Kill();
                        }
                        catch
                        {
                            // ignore cleanup errors
                        }
                    }
                }

                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true;
                    StopAll();
                };

                AppDomain.CurrentDomain.ProcessExit += (_, _) => StopAll();

                foreach (var appProject in appProjects)
                {
                    Log.Information("Starting {AppName} from {RelativePath}", appProject.Name, RelativePath(appProject));

                    var process = ProcessTasks.StartProcess(
                        toolPath: "dotnet",
                        arguments: $"run --project \"{appProject.Path}\" --configuration Debug --verbosity minimal",
                        workingDirectory: RootDirectory);

                    processes.Add(process);
                }

                try
                {
                    foreach (var process in processes)
                        process.WaitForExit();

                    var failed = processes.Where(p => p.ExitCode != 0).ToArray();
                    if (failed.Length != 0)
                        throw new Exception($"One or more apps exited with a non-zero exit code: {failed.Length}");
                }
                finally
                {
                    StopAll();
                }
            });
    }
}
