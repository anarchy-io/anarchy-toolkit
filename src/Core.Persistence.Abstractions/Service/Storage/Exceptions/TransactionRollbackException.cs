namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Error rolling back transaction.
/// </summary>
/// <param name="innerException">The underlying rollback failure.</param>
public class TransactionRollbackException(Exception innerException)
    : UnitOfWorkTransactionException("Error rolling back transaction", innerException);
