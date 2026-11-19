using Anarchy.Toolkit.Automation.Core.Template;
using Anarchy.Toolkit.Automation.Infrastructure.Helper;

namespace Anarchy.Toolkit.Automation.Core.Service;

/// <summary>
/// Writes a Fallout build host (<c>build/_build.csproj</c> and its entry point) on <c>Anarchy.Toolkit.Automation.Build</c>;
/// a null version leaves the package unversioned, for central package management.
/// </summary>
public static class BuildHostScaffolder
{
    /// <summary>
    /// Writes <c>build/_build.csproj</c> and <c>build/Program.cs</c>, whose <c>Build</c> class implements the toolkit's
    /// standard build components (NuGet, layering, dependency map, upgrades, Git hooks, boilerplate and Docker).
    /// </summary>
    /// <param name="root">The solution directory.</param>
    /// <param name="buildFalloutVersion">
    /// The version of <c>Anarchy.Toolkit.Automation.Build</c>, or <see langword="null"/> under central package management.
    /// </param>
    /// <exception cref="System.IO.IOException">Thrown when <c>build/_build.csproj</c> or <c>build/Program.cs</c> already exists.</exception>
    public static void Create(string root, string? buildFalloutVersion)
    {
        FileHelper.WriteNewText(Path.Combine(root, "build", "_build.csproj"), $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                {{ApplicationTemplates.PackageReference("Anarchy.Toolkit.Automation.Build", buildFalloutVersion)}}
              </ItemGroup>
            </Project>

            """);
        FileHelper.WriteNewText(Path.Combine(root, "build", "Program.cs"), """
            using Anarchy.Toolkit.Automation.Build.Component;
            using Anarchy.Toolkit.Automation.Build.Core.Entity.Build;
            using Anarchy.Toolkit.Automation.Build.Infrastructure.Extension;
            using Fallout.Common;

            class Build : FalloutBuild, INuGetBuild, ILayeringBuild, IDependencyMapBuild,
                          IDependencyUpgradeBuild, IGitHooksBuild, IBoilerplateBuild, IDockerBuild,
                          IAgentSkillsBuild
            {
                public IReadOnlyCollection<Artifact> Artifacts => [];
                public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>().Clean);
            }

            """);
    }
}
