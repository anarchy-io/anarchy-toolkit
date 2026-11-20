using Anarchy.Toolkit.Core.Entity;

namespace Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Repository.Storage.Exceptions;

/// <summary>
///     Thrown when storing entities violates a uniqueness constraint.
/// </summary>
/// <param name="duplicates">The entities that could not be stored because they already exist.</param>
/// <param name="innerException">The underlying storage failure, if any.</param>
public class DuplicatedEntityStorageException(
    IReadOnlyCollection<IEntity> duplicates,
    Exception? innerException = null)
    : PersistenceStorageException("Unable to store duplicated entities", innerException)
{
    /// <summary>
    ///     Gets the entities that could not be stored because they already exist.
    /// </summary>
    public IReadOnlyCollection<IEntity> Duplicates { get; } = duplicates;
}
