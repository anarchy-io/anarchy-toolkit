using System.Transactions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Base for failures that happen while running a unit-of-work session.
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The underlying failure, if any.</param>
public class UnitOfWorkSessionException(string message, Exception? innerException)
    : TransactionException(message, innerException)
{
}
