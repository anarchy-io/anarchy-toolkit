namespace Anarchy.Toolkit.Build.Cli.Core.Entity;

/// <summary>The toolkit package versions generated projects reference.</summary>
public sealed class ToolkitPackageVersions(IReadOnlyDictionary<string, string> versions)
{
    public const string BuildFallout = "Anarchy.Toolkit.Build.Fallout";

    public string this[string packageId] => versions.TryGetValue(packageId, out var version)
        ? version
        : throw new InvalidOperationException($"No version of {packageId} is known to this tool.");
}
