namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Error finishing the transaction session.
/// </summary>
/// <param name="innerException">The underlying failure (e.g. releasing the connection).</param>
public class SessionFinishException(Exception innerException)
    : UnitOfWorkTransactionException("Error finishing the transaction session", innerException);
