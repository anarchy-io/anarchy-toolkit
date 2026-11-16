using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

/// <summary>
///     Thrown when a requested entity does not exist in the storage.
/// </summary>
/// <param name="message">A message describing what was not found.</param>
/// <param name="internalException">The underlying failure, if any.</param>
public class EntityNotFoundException(string message, Exception? internalException = null)
    : NotFoundException(message, internalException);
