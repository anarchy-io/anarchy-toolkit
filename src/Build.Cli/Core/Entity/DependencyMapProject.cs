namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// A project of a <see cref="DependencyMap"/>, with its group and source size.
/// </summary>
/// <param name="Name">The project name.</param>
/// <param name="Group">Its layer, or a group outside the chain (e.g. <c>Tests</c>).</param>
/// <param name="SourceFiles">Number of <c>.cs</c> files, <c>bin</c>/<c>obj</c> excluded.</param>
/// <param name="SourceLines">Number of non-blank lines in those files.</param>
public sealed record DependencyMapProject(string Name, string Group, int SourceFiles, int SourceLines);
