namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>A solution project, as read by the build host, for the checks that work on plain inputs.</summary>
/// <param name="Name">The project name, e.g. <c>Infrastructure.Http</c>.</param>
/// <param name="Path">The full path of the project file.</param>
/// <param name="Directory">The full path of the project's directory.</param>
/// <param name="RelativePath">The project file's path relative to the solution directory.</param>
/// <param name="RootNamespace">The namespace the project owns (its name when it sets none).</param>
/// <param name="ProjectReferences">The <c>Include</c> of each <c>ProjectReference</c>, relative to <paramref name="Directory"/>.</param>
public sealed record SolutionProjectInfo(
    string Name,
    string Path,
    string Directory,
    string RelativePath,
    string RootNamespace,
    IReadOnlyCollection<string> ProjectReferences);
