namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a required environment variable is not set, empty or whitespace.
/// </summary>
/// <param name="variableName">The name of the missing environment variable.</param>
public class MissingEnvironmentVariableException(string variableName)
    : InvalidStateException($"Environment variable '{variableName}' is not set, empty or whitespace.")
{
    /// <summary>
    /// Gets the name of the missing environment variable.
    /// </summary>
    public string VariableName { get; } = variableName;
}
