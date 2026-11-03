namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Error committing transaction.
/// </summary>
public class TransactionCommitException(Exception innerException)
    : UnitOfWorkTransactionException("Error committing transaction", innerException);
