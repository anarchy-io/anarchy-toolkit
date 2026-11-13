using Anarchy.Toolkit.Build.Cli.Core.Service;
using AwesomeAssertions;
using System.Runtime.CompilerServices;
using System.Security;

namespace Anarchy.Toolkit.Tests.Unit.Build.Fallout.Component;

public class NuGetAssemblySelectionTest
{
    [Fact]
    public void BuildTargets_SelectNewestAssemblyAndPreservePackageMetadata()
    {
        const string build = "Fallout";
        var root = Path.Combine(Path.GetTempPath(), "nuget-selection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var targets = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SourcePath())!, "../../../../../src", "Build." + build,
                "buildTransitive", "Anarchy.Toolkit.Build." + build + ".targets"));
            static string Escape(string value) => SecurityElement.Escape(value)!;
            string MakeAssembly(string name, string version)
            {
                var dir = Path.Combine(root, name);
                Directory.CreateDirectory(dir);
                var project = Path.Combine(dir, "fixture.csproj");
                File.WriteAllText(project, $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>NuGet.Versioning</AssemblyName><AssemblyVersion>{version}</AssemblyVersion><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>");
                LocalProcessTasks.Require(root, "dotnet", "build", project, "--nologo", "-v:q");
                return Path.Combine(dir, "bin", "Debug", "net10.0", "NuGet.Versioning.dll");
            }

            var older = MakeAssembly("older", "1.0.0.0");
            var newer = MakeAssembly("newer", "999.0.0.0");
            var missing = Path.Combine(root, "NuGet.NoSdkCounterpart.dll");
            File.Copy(older, missing);
            foreach (var (package, expectSdk) in new[] { (older, true), (newer, false), (missing, false) })
            {
                var project = Path.Combine(root, "probe.proj");
                var output = Path.Combine(root, "result.txt");
                File.WriteAllText(project, $"""
                    <Project>
                      <ItemGroup><RuntimeCopyLocalItems Include="{Escape(package)}"><NuGetPackageId>NuGet.Versioning</NuGetPackageId><Marker>preserved</Marker></RuntimeCopyLocalItems></ItemGroup>
                      <Import Project="{Escape(targets)}" />
                      <Target Name="Probe" DependsOnTargets="AnarchyToolkit{build}UseSdkProvidedNuGetAssemblies">
                        <WriteLinesToFile File="{Escape(output)}" Lines="@(RuntimeCopyLocalItems->'%(Identity)|%(Marker)')" Overwrite="true" />
                      </Target>
                    </Project>
                    """);
                var run = LocalProcessTasks.Run(root, "dotnet", ["msbuild", project, "-nologo", "-t:Probe"]);
                run.ExitCode.Should().Be(0, "{0} {1}", run.Output, run.Error);
                var result = File.ReadAllLines(output).Should().ContainSingle().Subject;
                var parts = result.Split('|');
                parts[1].Should().Be("preserved");
                if (expectSdk) parts[0].Should().NotBe(package).And.EndWith("NuGet.Versioning.dll");
                else parts[0].Should().Be(package);
            }
            var emptyProject = Path.Combine(root, "empty.proj");
            File.WriteAllText(emptyProject, $"<Project><Import Project=\"{Escape(targets)}\" /><Target Name=\"Probe\" DependsOnTargets=\"AnarchyToolkit{build}UseSdkProvidedNuGetAssemblies\" /></Project>");
            var emptyRun = LocalProcessTasks.Run(root, "dotnet", ["msbuild", emptyProject, "-nologo", "-t:Probe"]);
            emptyRun.ExitCode.Should().Be(0, "{0} {1}", emptyRun.Output, emptyRun.Error);
        }
        finally { Directory.Delete(root, true); }
    }

    private static string SourcePath([CallerFilePath] string source = "") => source;
}
