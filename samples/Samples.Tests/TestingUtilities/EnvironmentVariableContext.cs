namespace Anarchy.Toolkit.Samples.Tests.TestingUtilities;

/// <summary>
/// Temporarily sets an environment variable for the duration of a test. Use within
/// <see cref="EnvironmentVariableCollection"/>, as it manages process-wide state.
/// </summary>
public sealed class EnvironmentVariableContext : IDisposable
{
    private readonly string _name;
    private readonly string? _originalValue;

    public EnvironmentVariableContext(string name, string? value)
    {
        _name = name;
        _originalValue = Environment.GetEnvironmentVariable(_name, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(_name, value, EnvironmentVariableTarget.Process);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(_name, _originalValue);
}

/// <summary>
/// Serializes test classes that mutate process environment variables through <see cref="EnvironmentVariableContext"/>.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentVariableCollection
{
    public const string Name = "EnvironmentVariables";
}
