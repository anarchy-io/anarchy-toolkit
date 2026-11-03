namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Observability.Serilog.TestingUtilities;

/// <summary>
/// Serializes test classes that mutate process environment variables through <see cref="EnvironmentVariableContext"/>.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentVariableCollection
{
    public const string Name = "EnvironmentVariables";
}
