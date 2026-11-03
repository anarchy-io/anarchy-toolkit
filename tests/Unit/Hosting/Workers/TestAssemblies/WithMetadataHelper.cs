using System.Reflection;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;

/// <summary>
/// Replaces the legacy <c>TestAssemblies.WithMetadata</c> helper assembly: this test assembly embeds
/// <c>TestAssemblies/build.meta.json</c> as <c>Properties.build.meta.json</c> (see the csproj).
/// </summary>
public static class WithMetadataHelper
{
    public static Assembly Assembly => typeof(WithMetadataHelper).GetTypeInfo().Assembly;
    public static string Version => Assembly.GetName().Version!.ToString();
    public static string AssemblyName => Assembly.GetName().Name!;
}
