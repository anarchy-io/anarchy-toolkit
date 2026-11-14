namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// A project reference between two projects of a <see cref="DependencyMap"/>.
/// </summary>
/// <param name="From">The referencing project.</param>
/// <param name="To">The referenced project.</param>
public sealed record DependencyMapReference(string From, string To);
