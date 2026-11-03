namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for failures of an external dependency (database, message broker, secret store, identity provider,
/// external process...). The original failure is kept as the inner exception.
/// </summary>
public class ExternalDependencyException(string message, Exception? innerException = null)
    : Exception(message, innerException);
