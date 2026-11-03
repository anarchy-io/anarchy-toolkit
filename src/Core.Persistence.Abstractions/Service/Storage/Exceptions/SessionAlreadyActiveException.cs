namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Transaction session is already active, unable to start.
/// </summary>
public class SessionAlreadyActiveException()
    : UnitOfWorkTransactionException("Transaction session is already active, unable to start");
