namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Error committing transaction.
/// </summary>
/// <param name="innerException">The underlying commit failure.</param>
public class TransactionCommitException(Exception innerException)
    : UnitOfWorkTransactionException("Error committing transaction", innerException);
