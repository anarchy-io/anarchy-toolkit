namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

/// <summary>
///     Thrown when the entities passed to a batch save produce no valid query input (e.g. all were filtered out
///     during conversion).
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The underlying failure, if any.</param>
public class EmptyInputListForBatchSaveStorageException(
    string message = "Entity list provided to the Storage's batch save method was not converted in a valid query input list",
    Exception? innerException = null)
    : EmptyEntityListForBatchSaveStorageException(message, innerException);
