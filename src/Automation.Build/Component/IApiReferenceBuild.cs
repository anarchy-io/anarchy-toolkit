using Anarchy.Toolkit.Automation.Build.Component.Tasks;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Solutions;
using Serilog;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>
/// Generates an API reference website with <see href="https://dotnet.github.io/docfx/">DocFX</see> from the compiled
/// assemblies of the solution's packages and their XML documentation, so the projects are compiled only once, by
/// <see cref="IStandardBuild.Compile"/>. The site (template, landing page, table of contents and output folder) is
/// configured by the <c>build</c> section of <see cref="ApiReferenceConfig"/>; the API metadata is generated into
/// <see cref="ApiReferenceMetadataOutput"/>, where that section should pick it up.
/// </summary>
/// <example>
/// <code language="csharp">
/// partial class Build : FalloutBuild, IApiReferenceBuild
/// {
///     public static int Main() => Execute&lt;Build&gt;(x => x.As&lt;IStandardBuild&gt;().Compile);
/// }
/// </code>
/// Then run <c>fallout generate-api-reference</c>.
/// </example>
public interface IApiReferenceBuild : IStandardBuild
{
    /// <summary>
    /// The DocFX configuration (<c>docfx.json</c>) of the API reference site. Parameter <c>--api-reference-config</c>;
    /// defaults to <c>docs-api/docfx.json</c>.
    /// </summary>
    [Parameter("DocFX configuration of the API reference site; defaults to docs-api/docfx.json")]
    AbsolutePath ApiReferenceConfig => TryGetValue(() => ApiReferenceConfig) ?? RootDirectory / "docs-api" / "docfx.json";

    /// <summary>
    /// Where the API metadata (YAML) is generated, by default an <c>api</c> folder next to
    /// <see cref="ApiReferenceConfig"/>.
    /// </summary>
    AbsolutePath ApiReferenceMetadataOutput => ApiReferenceConfig.Parent / "api";

    /// <summary>
    /// Whether a project is documented. By default the packable projects under
    /// <see cref="IDefaultBuild.SourceDirectory"/>, except .NET tools.
    /// </summary>
    /// <param name="project">The solution project to consider.</param>
    /// <returns><see langword="true"/> to document the project; otherwise <see langword="false"/>.</returns>
    bool IsApiReferenceProject(Project project) =>
        IsInDirectory(project.Path, SourceDirectory)
        && string.Equals(project.GetProperty("IsPackable"), "true", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(project.GetProperty("PackAsTool"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Generates the API reference site after <see cref="IStandardBuild.Compile"/>: resolves the compiled assemblies of
    /// every project selected by <see cref="IsApiReferenceProject"/>, generates their metadata into
    /// <see cref="ApiReferenceMetadataOutput"/> and builds the site configured by <see cref="ApiReferenceConfig"/>.
    /// Fails when no project is selected or DocFX reports errors.
    /// </summary>
    Target GenerateApiReference => d => d
        .Description("Generates the DocFX API reference website from the compiled packages")
        .DependsOn(Compile)
        .Executes(async () => {
            var projects = Solution.AllProjects
                .Where(IsApiReferenceProject)
                .Select(project => new ApiReferenceProject(
                    project.Path,
                    string.IsNullOrEmpty(project.GetProperty("TargetFramework"))
                        ? project.GetProperty("TargetFrameworks")?.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                        : null))
                .ToList();
            Assert.NotEmpty(projects, "No projects to document");

            var workingDirectory = TemporaryDirectory / "api-reference";
            var (assemblies, references) = ApiReferenceTasks.ResolveAssemblies(projects, Configuration, workingDirectory);
            Log.Information("Documenting {Assemblies} assemblies ({References} references)", assemblies.Count, references.Count);

            await ApiReferenceTasks.Generate(ApiReferenceConfig, ApiReferenceMetadataOutput, assemblies, references);
        });
}
