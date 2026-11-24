using Anarchy.Toolkit.Automation.Core.Entity;
using Anarchy.Toolkit.Automation.Core.Template;
using AwesomeAssertions;
using System.Diagnostics;

namespace Anarchy.Toolkit.Tests.EndToEnd.Automation.Build.Cli;

/// <summary>Compiles real generated applications against the checked-out toolkit, without a published package feed.</summary>
public sealed class GeneratedEntryPointBuildTest
{
    [Fact]
    public async Task GeneratedEntryPoints_CompileForEveryHostKind()
    {
        var root = Directory.CreateTempSubdirectory("anarchy-entrypoints-").FullName;
        try
        {
            var repository = FindRepository();
            var applications = new[] {
                new ApplicationSpec(ApplicationKind.Api, "Orders") { Container = true, ApiLambda = true },
                new ApplicationSpec(ApplicationKind.Ui, "Admin") { Container = true },
                new ApplicationSpec(ApplicationKind.Job, "Once") { JobMode = JobExecutionMode.Once, EventLambda = true },
                new ApplicationSpec(ApplicationKind.Job, "Worker") { JobMode = JobExecutionMode.Worker, EventLambda = true }
            };
            foreach (var app in applications)
            {
                var directory = Path.Combine(root, app.Name);
                var context = new ApplicationContext(app, app.Name, $"App.{app.Name}", null, (5000, 5001), null) {
                    LayeringMode = LayeringMode.Folder
                };
                var files = ApplicationTemplates.Render(context);
                foreach (var (path, content) in files)
                {
                    var destination = Path.Combine(directory, path);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    var actual = path.EndsWith(".csproj", StringComparison.Ordinal)
                        ? content.Replace($"<PackageReference Include=\"{app.PackageId}\" />",
                            $"<ProjectReference Include=\"{Path.Combine(repository, "src", PackageProject(app), PackageProject(app) + ".csproj")}\" />",
                            StringComparison.Ordinal)
                        : content;
                    await File.WriteAllTextAsync(destination, actual, TestContext.Current.CancellationToken);
                }

                var process = new ProcessStartInfo("dotnet") { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var argument in new[] { "build", $"{app.Name}.csproj", "-c", "Release", "-p:TreatWarningsAsErrors=true" })
                    process.ArgumentList.Add(argument);
                using var build = Process.Start(process) ?? throw new InvalidOperationException("Could not start dotnet build.");
                var stdout = build.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
                var stderr = build.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
                await build.WaitForExitAsync(TestContext.Current.CancellationToken);
                build.ExitCode.Should().Be(0, $"{app.Kind} {app.Name} must compile: {await stdout}\n{await stderr}");
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string PackageProject(ApplicationSpec app) => app.Kind switch {
        ApplicationKind.Api => "Web",
        ApplicationKind.Ui => "Web.Ui",
        ApplicationKind.Job => "Hosting",
        _ => throw new ArgumentOutOfRangeException(nameof(app))
    };

    private static string FindRepository()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Anarchy.Toolkit.sln"))) return directory.FullName;
        throw new InvalidOperationException("The toolkit solution was not found.");
    }
}
