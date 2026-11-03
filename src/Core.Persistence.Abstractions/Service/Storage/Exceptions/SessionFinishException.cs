namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Error finishing the transaction session.
/// </summary>
public class SessionFinishException(Exception innerException)
    : UnitOfWorkTransactionException("Error finishing the transaction session", innerException);
