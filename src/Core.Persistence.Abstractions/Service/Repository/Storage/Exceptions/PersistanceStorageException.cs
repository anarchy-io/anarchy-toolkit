namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

public class PersistanceStorageException(
    string message = "Problem when persisting data to the storage",
    Exception? innerException = null)
    : StorageException(message, innerException);
