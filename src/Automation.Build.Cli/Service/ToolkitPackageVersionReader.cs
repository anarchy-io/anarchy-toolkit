using Anarchy.Toolkit.Automation.Core.Entity;
using System.Reflection;

namespace Anarchy.Toolkit.Automation.Build.Cli.Service;

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
    /// <param name="assembly">The assembly whose <see cref="AssemblyMetadataAttribute"/> entries are read.</param>
    /// <returns>The versions found; empty if the assembly carries none.</returns>
    public static ToolkitPackageVersions FromAssembly(Assembly assembly) => new(
        assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => attribute.Key.StartsWith(Prefix, StringComparison.Ordinal) && !string.IsNullOrEmpty(attribute.Value))
            .ToDictionary(attribute => attribute.Key[Prefix.Length..], attribute => attribute.Value!));
}
