using Anarchy.Toolkit.Automation.Build.Cli.Service;
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
                new ApplicationSpec(ApplicationKind.Api, "Catalog") { Container = true },
                new ApplicationSpec(ApplicationKind.Api, "Unthemed", SwaggerTheme: false),
                new ApplicationSpec(ApplicationKind.Api, "Bare") { Swagger = false },
                new ApplicationSpec(ApplicationKind.Ui, "Admin") { Container = true },
                new ApplicationSpec(ApplicationKind.Job, "Once") { JobMode = JobExecutionMode.Once, EventLambda = true },
                new ApplicationSpec(ApplicationKind.Job, "Worker") { JobMode = JobExecutionMode.Worker, EventLambda = true },
                new ApplicationSpec(ApplicationKind.Job, "Plain") { JobMode = JobExecutionMode.Once }
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
                        ? app.PackageIds.Aggregate(content, (project, packageId) => project.Replace($"<PackageReference Include=\"{packageId}\" />",
                            $"<ProjectReference Include=\"{Path.Combine(repository, "src", PackageProject(packageId), PackageProject(packageId) + ".csproj")}\" />",
                            StringComparison.Ordinal))
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
                if (app.Kind == ApplicationKind.Api)
                    File.Exists(Path.Combine(directory, "bin", "Release", "net10.0", "Swashbuckle.AspNetCore.SwaggerGen.dll"))
                        .Should().Be(app.Swagger, $"{app.Name} references Swashbuckle only with Swagger");
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SharedSettingsSolution_CompilesAndCopiesTheSettingsIntoApplicationsOnly()
    {
        var root = Directory.CreateTempSubdirectory("anarchy-shared-").FullName;
        try
        {
            var repository = FindRepository();
            var prompter = new ScriptedPrompter();
            var application = new ToolApplication(prompter, Fixture.ToolkitSolutionFixture.Versions, root);
            application.Run(["bootstrap", "MyApp", "--output", root, "--no-tests"], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));
            foreach (var (kind, name) in new[] { ("api", "Orders"), ("job", "Reports"), ("ui", "Admin") })
                application.Run(["scaffold", kind, name], interactive: false).Should().Be(0, string.Join("\n", prompter.Errors));

            // Compile against the checked-out toolkit: each toolkit package reference becomes a project reference.
            foreach (var project in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories))
            {
                var content = System.Text.RegularExpressions.Regex.Replace(await File.ReadAllTextAsync(project, TestContext.Current.CancellationToken),
                    "<PackageReference Include=\"Anarchy\\.Toolkit\\.([^\"]+)\" />",
                    match => $"<ProjectReference Include=\"{Path.Combine(repository, "src", match.Groups[1].Value, match.Groups[1].Value + ".csproj")}\" />");
                await File.WriteAllTextAsync(project, content, TestContext.Current.CancellationToken);
            }

            var apps = new[] { "src/Apps/Apis/Orders/Orders.csproj", "src/Apps/Jobs/Reports/Reports.csproj", "src/Apps/Ui/Admin/Admin.csproj" };
            foreach (var app in apps)
            {
                var process = new ProcessStartInfo("dotnet") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var argument in new[] { "build", app, "-c", "Release", "-p:TreatWarningsAsErrors=true" })
                    process.ArgumentList.Add(argument);
                using var build = Process.Start(process) ?? throw new InvalidOperationException("Could not start dotnet build.");
                var stdout = build.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
                var stderr = build.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
                await build.WaitForExitAsync(TestContext.Current.CancellationToken);
                build.ExitCode.Should().Be(0, $"{app} must compile: {await stdout}\n{await stderr}");

                var output = Path.Combine(root, Path.GetDirectoryName(app)!, "bin", "Release", "net10.0");
                File.Exists(Path.Combine(output, "appsettings.json")).Should().BeTrue(app);
                File.Exists(Path.Combine(output, "appsettings.Development.json")).Should().BeTrue(app);
            }

            foreach (var layer in new[] { "Core", "Infrastructure", "Bootstrap", "Runtime" })
            {
                var output = Path.Combine(root, "src", "Library", layer, "bin", "Release", "net10.0");
                Directory.Exists(output).Should().BeTrue(layer);
                Directory.EnumerateFiles(output, "appsettings*.json").Should().BeEmpty(layer);
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string PackageProject(string packageId) => packageId["Anarchy.Toolkit.".Length..];

    private static string FindRepository()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Anarchy.Toolkit.sln"))) return directory.FullName;
        throw new InvalidOperationException("The toolkit solution was not found.");
    }
}
