namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Error rolling back transaction.
/// </summary>
public class TransactionRollbackException(Exception innerException)
    : UnitOfWorkTransactionException("Error rolling back transaction", innerException);
