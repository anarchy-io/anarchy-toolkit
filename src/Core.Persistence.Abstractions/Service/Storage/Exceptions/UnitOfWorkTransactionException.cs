using System.Transactions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Base for failures while managing a unit-of-work transaction or session lifecycle.
/// </summary>
public class UnitOfWorkTransactionException(string message, Exception? innerException = null)
    : TransactionException(message, innerException);
