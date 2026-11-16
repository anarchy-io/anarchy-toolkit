namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Transaction session is already active, unable to start.
/// </summary>
/// <remarks>Unit-of-work sessions cannot be nested within the same async flow.</remarks>
public class SessionAlreadyActiveException()
    : UnitOfWorkTransactionException("Transaction session is already active, unable to start");
