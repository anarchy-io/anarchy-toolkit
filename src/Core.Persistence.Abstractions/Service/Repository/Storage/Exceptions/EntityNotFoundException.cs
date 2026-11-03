using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

public class EntityNotFoundException(string message, Exception? internalException = null)
    : NotFoundException(message, internalException);
