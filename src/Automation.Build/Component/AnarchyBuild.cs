using Fallout.Common;

namespace Anarchy.Toolkit.Automation.Build.Component;

/// <summary>The base host for Anarchy automation, powered by <see href="https://fallout.build/">Fallout Build</see>.</summary>
/// <remarks>
/// Compose the toolkit's build-component interfaces on a derived class. Fallout supplies execution, parameters,
/// target discovery and extension hooks; its advanced APIs remain available without a second execution engine.
/// </remarks>
public abstract class AnarchyBuild : FalloutBuild
{
}
