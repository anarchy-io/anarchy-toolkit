using Anarchy.Toolkit.Build.Cli.Infrastructure.Helper;

namespace Anarchy.Toolkit.Build.Cli.Core.Service;

/// <summary>Writes a Fallout build host (<c>build/_build.csproj</c> and its entry point) on <c>Anarchy.Toolkit.Build.Fallout</c>.</summary>
public static class BuildHostScaffolder
{
    public static void Create(string root, string buildFalloutVersion)
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
                <PackageReference Include="Anarchy.Toolkit.Build.Fallout" Version="{{buildFalloutVersion}}" />
              </ItemGroup>
            </Project>

            """);
        FileHelper.WriteNewText(Path.Combine(root, "build", "Program.cs"), """
            using Anarchy.Toolkit.Build.Fallout.Component;
            using Anarchy.Toolkit.Build.Fallout.Core.Entity.Build;
            using Anarchy.Toolkit.Build.Fallout.Infrastructure.Extension;
            using Fallout.Common;

            class Build : FalloutBuild, INuGetBuild, ILayeringBuild, IDependencyMapBuild,
                          IDependencyUpgradeBuild, IGitHooksBuild, IBoilerplateBuild
            {
                public IReadOnlyCollection<Artifact> Artifacts => [];
                public static int Main() => Execute<Build>(x => x.As<IDefaultBuild>().Clean);
            }

            """);
    }
}
