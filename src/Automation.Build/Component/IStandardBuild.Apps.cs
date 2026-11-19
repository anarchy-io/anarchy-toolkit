using Fallout.Common;
using Fallout.Common.Tooling;
using Fallout.Common.Tools.DotNet;
using Serilog;
using static Fallout.Common.Tools.DotNet.DotNetTasks;

namespace Anarchy.Toolkit.Automation.Build.Component;

// `Configuration` would otherwise resolve to this project's Configuration namespace (see the architecture guide).
using Configuration = Anarchy.Toolkit.Automation.Build.Core.ValueSet.Build.Configuration;

public partial interface IStandardBuild
{
    /// <summary>
    /// The runnable app <see cref="RunApp"/> runs (<c>--app-name</c>): the name of a project under <c>src</c> with
    /// <c>OutputType</c> <c>Exe</c>, case-insensitive. Also the app <see cref="IContainerBuild"/> publishes.
    /// </summary>
    [Parameter("Runnable app name to execute (any project under the source root with OutputType=Exe)")]
    string? AppName => TryGetValue(() => AppName);

    /// <summary>
    /// The runnable apps <see cref="RunApps"/> runs, comma-separated (<c>--app-names</c>).
    /// </summary>
    [Parameter("Runnable app names to execute in parallel (comma-separated)")]
    string? AppNames => TryGetValue(() => AppNames);

    /// <summary>
    /// Runs <see cref="AppName"/> with <c>dotnet run</c> in the <c>Debug</c> configuration.
    /// </summary>
    /// <remarks>Without <see cref="AppName"/>, or with an unknown one, it lists the runnable apps and fails.</remarks>
    Target RunApp => d => d
        .Description("Builds the solution and runs one runnable app from the source root")
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
                .SetVerbosity(ToolVerbosity));
        });

    /// <summary>
    /// Runs every app in <see cref="AppNames"/> in parallel with <c>dotnet run</c> in the <c>Debug</c>
    /// configuration, stopping all of them on Ctrl+C and failing when any exits with a non-zero code.
    /// </summary>
    /// <remarks>Without <see cref="AppNames"/>, or with an unknown one, it lists the runnable apps and fails.</remarks>
    Target RunApps => d => d
        .Description("Builds the solution and runs multiple runnable apps from the source root in parallel")
        .Executes(() => {
            if (string.IsNullOrWhiteSpace(AppNames))
            {
                Log.Warning("No apps specified. Pass --AppNames <name1,name2,...> to run multiple apps");
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
                    arguments: $"run --project \"{appProject.Path}\" --configuration Debug --verbosity {ToolVerbosity}",
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
