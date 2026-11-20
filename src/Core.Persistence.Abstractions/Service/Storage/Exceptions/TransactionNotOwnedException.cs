namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Thrown when code that joined a caller's transaction tries to begin, commit or roll back a transaction.
/// </summary>
/// <remarks>
/// The transaction belongs to the caller that started the session; a participant (see
/// <see cref="IUnitOfWork.RunInJoinedSessionAsync{TResult}"/>) signals failure by throwing instead.
/// </remarks>
public class TransactionNotOwnedException()
    : UnitOfWorkTransactionException("The transaction is owned by the caller that started the session");
