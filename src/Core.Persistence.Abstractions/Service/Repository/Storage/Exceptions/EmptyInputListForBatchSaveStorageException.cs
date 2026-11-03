namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

public class EmptyInputListForBatchSaveStorageException(
    string message = "Entity list provided to the Storage's batch save method was not converted in a valid query input list",
    Exception? innerException = null)
    : EmptyEntityListForBatchSaveStorageException(message, innerException);
