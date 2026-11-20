namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

/// <summary>
///     Thrown when a batch save is called with an empty entity list.
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The underlying failure, if any.</param>
public class EmptyEntityListForBatchSaveStorageException(
    string message = "Empty entity list provided to the Storage's batch save method",
    Exception? innerException = null)
    : PersistenceStorageException(message, innerException);
