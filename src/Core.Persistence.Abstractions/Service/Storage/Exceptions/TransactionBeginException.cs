namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Error starting a new transaction.
/// </summary>
public class TransactionBeginException(Exception innerException)
    : UnitOfWorkTransactionException("Error starting a new transaction", innerException);
