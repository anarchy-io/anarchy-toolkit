namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// The toolkit package versions generated projects reference.
/// </summary>
/// <param name="versions">The version of each known toolkit package, keyed by package ID.</param>
public sealed class ToolkitPackageVersions(IReadOnlyDictionary<string, string> versions)
{
    /// <summary>
    /// The package ID of the Fallout build components every build host references.
    /// </summary>
    public const string BuildFallout = "Anarchy.Toolkit.Build.Fallout";

    /// <summary>
    /// The version of a toolkit package.
    /// </summary>
    /// <param name="packageId">The package ID, e.g. <see cref="BuildFallout"/>.</param>
    /// <returns>The version of <paramref name="packageId"/>.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when no version of <paramref name="packageId"/> is known.</exception>
    public string this[string packageId] => versions.TryGetValue(packageId, out var version)
        ? version
        : throw new InvalidOperationException($"No version of {packageId} is known to this tool.");
}
