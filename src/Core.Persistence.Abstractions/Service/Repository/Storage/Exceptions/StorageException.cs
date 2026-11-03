namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

public class StorageException(string message, Exception? innerException = null)
    : Exception(message, innerException);
