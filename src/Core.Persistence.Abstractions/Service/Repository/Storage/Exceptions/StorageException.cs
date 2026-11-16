namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

/// <summary>
///     Base for failures reported by storage repositories.
/// </summary>
/// <param name="message">A message describing the failure.</param>
/// <param name="innerException">The underlying storage failure, if any.</param>
public class StorageException(string message, Exception? innerException = null)
    : Exception(message, innerException);
