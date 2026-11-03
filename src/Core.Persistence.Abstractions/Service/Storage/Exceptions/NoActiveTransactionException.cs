using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage.Exceptions;

/// <summary>
/// Thrown when committing or rolling back without an active transaction.
/// </summary>
public class NoActiveTransactionException() : InvalidStateException("No active transaction");
