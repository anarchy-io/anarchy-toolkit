namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

public class EmptyEntityListForBatchSaveStorageException(
    string message = "Empty entity list provided to the Storage's batch save method",
    Exception? innerException = null)
    : PersistanceStorageException(message, innerException);
