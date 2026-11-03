
namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Observability.Serilog.TestingUtilities;

/// <summary>
/// Allows temporarily setting an environment variable for the duration of a test, and ensures that the environment
/// variable is cleaned up after the test is completed.
/// </summary>
/// <remarks>Be mindful that this class manages static state. When using this class, you should limit it running concurrently
/// with other tests that do a similar thing. You can use xunit's
/// <see href="https://xunit.net/docs/running-tests-in-parallel#custom-test-collections">CollectionAttribute</see> for this.
/// </remarks>
public sealed class EnvironmentVariableContext : IDisposable
{
    private readonly string _name;
    private readonly string? _originalValue;

    /// <summary>
    /// Sets the environment variable with the specified name and value for the duration of the context.
    /// </summary>
    /// <param name="name">The name of the Environment Variable.</param>
    /// <param name="value">The value of the Environment Variable.</param>
    public EnvironmentVariableContext(string name, string? value)
    {
        _name = name;

        _originalValue = Environment.GetEnvironmentVariable(_name, EnvironmentVariableTarget.Process);

        Environment.SetEnvironmentVariable(_name, value, EnvironmentVariableTarget.Process);
    }

    /// <inheritdoc/>
    public void Dispose() => Environment.SetEnvironmentVariable(_name, _originalValue);
}
