namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>
/// The toolkit package versions generated projects reference.
/// </summary>
public sealed class ToolkitPackageVersions(IReadOnlyDictionary<string, string> versions)
{
    /// <summary>
    /// The package ID of the Fallout build components every build host references.
    /// </summary>
    public const string BuildFallout = "Anarchy.Toolkit.Build.Fallout";

    /// <summary>
    /// The version of a toolkit package.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no version of <paramref name="packageId"/> is known.</exception>
    public string this[string packageId] => versions.TryGetValue(packageId, out var version)
        ? version
        : throw new InvalidOperationException($"No version of {packageId} is known to this tool.");
}
