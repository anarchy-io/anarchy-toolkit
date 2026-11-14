using Anarchy.Toolkit.Build.Cli.Core.Entity;
using System.Reflection;

namespace Anarchy.Toolkit.Build.Cli.Tool.Service;

/// <summary>
/// Reads the toolkit package versions embedded at build time as <c>AnarchyToolkitPackage:&lt;PackageId&gt;</c>
/// assembly metadata (see the project file).
/// </summary>
public static class ToolkitPackageVersionReader
{
    private const string Prefix = "AnarchyToolkitPackage:";

    /// <summary>
    /// Collects the package versions from <paramref name="assembly"/>, keyed by package ID and ignoring empty values.
    /// </summary>
    public static ToolkitPackageVersions FromAssembly(Assembly assembly) => new(
        assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => attribute.Key.StartsWith(Prefix, StringComparison.Ordinal) && !string.IsNullOrEmpty(attribute.Value))
            .ToDictionary(attribute => attribute.Key[Prefix.Length..], attribute => attribute.Value!));
}
