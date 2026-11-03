using System.Reflection;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;

/// <summary>
/// Replaces the legacy <c>TestAssemblies.WithoutMetadata</c> helper assembly: any assembly that does not embed a
/// <c>Properties.build.meta.json</c> resource will do, so the core library is used.
/// </summary>
public static class WithoutMetadataHelper
{
    public static Assembly Assembly => typeof(object).Assembly;
}
