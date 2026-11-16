namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Thrown when a required argument (string or collection) is null, empty or whitespace.
/// </summary>
/// <param name="paramName">The name of the empty argument.</param>
/// <param name="message">Optional custom message; a default message mentioning <paramref name="paramName"/> is used otherwise.</param>
public class EmptyArgumentException(string paramName, string? message = null)
    : InvalidArgumentException(message ?? $"Argument '{paramName}' cannot be null, empty or whitespace.", paramName);
