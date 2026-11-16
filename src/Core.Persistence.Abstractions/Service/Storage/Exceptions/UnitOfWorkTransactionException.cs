using System.Transactions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Base for failures while managing a unit-of-work transaction or session lifecycle.
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The underlying failure, if any.</param>
public class UnitOfWorkTransactionException(string message, Exception? innerException = null)
    : TransactionException(message, innerException);
