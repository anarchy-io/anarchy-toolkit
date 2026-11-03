namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Thrown when the callback executed within a unit-of-work session fails. The original failure is the inner
/// exception.
/// </summary>
public class SessionCallbackException(Exception innerException)
    : UnitOfWorkSessionException("Failed to execute session callback", innerException);
