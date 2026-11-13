using Anarchy.Toolkit.Build.Cli.Core.Entity;
using System.Reflection;

namespace Anarchy.Toolkit.Build.Cli.Tool.Service;

/// <summary>Reads the toolkit package versions embedded at build time (see the project file).</summary>
public static class ToolkitPackageVersionReader
{
    private const string Prefix = "AnarchyToolkitPackage:";

    public static ToolkitPackageVersions FromAssembly(Assembly assembly) => new(
        assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => attribute.Key.StartsWith(Prefix, StringComparison.Ordinal) && !string.IsNullOrEmpty(attribute.Value))
            .ToDictionary(attribute => attribute.Key[Prefix.Length..], attribute => attribute.Value!));
}
