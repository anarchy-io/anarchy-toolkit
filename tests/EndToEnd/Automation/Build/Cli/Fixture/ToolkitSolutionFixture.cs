using Anarchy.Toolkit.Automation.Build.Cli.Service;
using Anarchy.Toolkit.Automation.Core.Entity;

[assembly: AssemblyFixture(typeof(Anarchy.Toolkit.Tests.EndToEnd.Automation.Build.Cli.Fixture.ToolkitSolutionFixture))]

namespace Anarchy.Toolkit.Tests.EndToEnd.Automation.Build.Cli.Fixture;

/// <summary>
/// Creates a baseline through the real CLI once; scaffold scenarios copy it into their own writable workspace.
/// Bootstrap scenarios still create their solutions themselves, and scaffold operations still use the real SDK.
/// </summary>
public sealed class ToolkitSolutionFixture : IDisposable
{
    public static readonly ToolkitPackageVersions Versions = new(new Dictionary<string, string> {
        ["Anarchy.Toolkit.Automation.Build"] = "1.2.3",
        ["Anarchy.Toolkit.Runtime.Web"] = "2.0.0",
        ["Anarchy.Toolkit.Runtime.Web.Aws"] = "2.2.0",
        ["Anarchy.Toolkit.Runtime.Web.Swagger"] = "2.3.0",
        ["Anarchy.Toolkit.Runtime.Web.Ui"] = "2.1.0",
        ["Anarchy.Toolkit.Runtime"] = "3.0.0",
        ["Anarchy.Toolkit.Runtime.Aws"] = "3.1.0",
        ["Anarchy.Toolkit.Bootstrap"] = "4.0.0",
        ["Anarchy.Toolkit.Runtime.Web.MockServer.Ui"] = "5.0.0",
        ["Anarchy.Toolkit.Testing"] = "6.0.0",
        ["Anarchy.Toolkit.Testing.Runtime.Web"] = "6.1.0",
        ["Anarchy.Toolkit.Testing.MockServer"] = "6.2.0"
    });

    private readonly string _root = Path.Combine(Path.GetTempPath(), "anarchy-baseline-" + Guid.NewGuid());

    public ToolkitSolutionFixture()
    {
        var prompter = new ScriptedPrompter();
        try
        {
            var exitCode = new ToolApplication(prompter, Versions, Path.GetTempPath())
                .Run(["bootstrap", "MyApp", "--output", _root, "--no-tests", "--isolated-app-settings"], interactive: false);
            if (exitCode != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, prompter.Errors));
        }
        catch
        {
            TestDirectory.Delete(_root);
            throw;
        }
    }

    public void CopyTo(string root)
    {
        Directory.CreateDirectory(root);
        foreach (var directory in Directory.EnumerateDirectories(_root, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(root, Path.GetRelativePath(_root, directory)));
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(root, Path.GetRelativePath(_root, file)));
    }

    /// <summary>Seeds an existing library solution without invoking bootstrap or its SDK commands.</summary>
    public static void CreateLibraryWorkspace(string root, SolutionLayout layout)
    {
        var project = layout.LibraryProjectPath("Core");
        var path = Path.Combine(root, project);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        Directory.CreateDirectory(Path.Combine(root, ".fallout"));
        File.WriteAllText(Path.Combine(root, ".fallout", "parameters.json"), """{ "Solution": "MyApp.sln" }""");
        File.WriteAllText(Path.Combine(root, "MyApp.sln"), $$"""
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Core", "{{project.Replace('/', '\\')}}", "{DF85316E-5A35-45C3-8BD0-890423245A60}"
            EndProject
            Global
              GlobalSection(SolutionConfigurationPlatforms) = preSolution
                Debug|Any CPU = Debug|Any CPU
                Release|Any CPU = Release|Any CPU
              EndGlobalSection
              GlobalSection(ProjectConfigurationPlatforms) = postSolution
                {DF85316E-5A35-45C3-8BD0-890423245A60}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
                {DF85316E-5A35-45C3-8BD0-890423245A60}.Debug|Any CPU.Build.0 = Debug|Any CPU
                {DF85316E-5A35-45C3-8BD0-890423245A60}.Release|Any CPU.ActiveCfg = Release|Any CPU
                {DF85316E-5A35-45C3-8BD0-890423245A60}.Release|Any CPU.Build.0 = Release|Any CPU
              EndGlobalSection
            EndGlobal
            """);
    }

    public void Dispose() => TestDirectory.Delete(_root);
}
