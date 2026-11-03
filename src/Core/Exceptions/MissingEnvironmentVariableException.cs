namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a required environment variable is not set, empty or whitespace.
/// </summary>
public class MissingEnvironmentVariableException(string variableName)
    : InvalidStateException($"Environment variable '{variableName}' is not set, empty or whitespace.")
{
    public string VariableName { get; } = variableName;
}
