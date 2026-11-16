namespace Anarchy.Toolkit.Core.Exceptions;

/// <summary>
/// Base for failures of an external dependency (database, message broker, secret store, identity provider,
/// external process...). The original failure is kept as the inner exception.
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The original exception raised by the dependency, if any.</param>
public class ExternalDependencyException(string message, Exception? innerException = null)
    : Exception(message, innerException);
