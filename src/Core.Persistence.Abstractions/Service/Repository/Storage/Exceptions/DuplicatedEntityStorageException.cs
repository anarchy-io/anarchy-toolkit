using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

public class DuplicatedEntityStorageException(
    IReadOnlyCollection<IEntity> duplicates,
    Exception? innerException = null)
    : PersistanceStorageException("Unable to store duplicated entities", innerException)
{
    public IReadOnlyCollection<IEntity> Duplicates { get; } = duplicates;
}
