namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a required argument (string or collection) is null, empty or whitespace.
/// </summary>
public class EmptyArgumentException(string paramName, string? message = null)
    : InvalidArgumentException(message ?? $"Argument '{paramName}' cannot be null, empty or whitespace.", paramName);
