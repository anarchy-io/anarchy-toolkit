namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Thrown when beginning a transaction in a session that has already started one.
/// </summary>
/// <remarks>A session can start only one transaction; the existing transaction is left untouched.</remarks>
public class TransactionAlreadyActiveException()
    : UnitOfWorkTransactionException("The session has already started a transaction");
