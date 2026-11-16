namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

/// <summary>
///     Thrown when data cannot be persisted to the storage.
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The underlying storage failure, if any.</param>
public class PersistanceStorageException(
    string message = "Problem when persisting data to the storage",
    Exception? innerException = null)
    : StorageException(message, innerException);
