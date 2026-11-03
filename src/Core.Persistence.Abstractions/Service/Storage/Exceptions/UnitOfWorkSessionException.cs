using System.Transactions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

public class UnitOfWorkSessionException(string message, Exception? innerException)
    : TransactionException(message, innerException)
{
}
