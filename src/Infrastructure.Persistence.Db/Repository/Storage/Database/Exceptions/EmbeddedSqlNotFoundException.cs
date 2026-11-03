using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Exceptions;

/// <summary>
/// Thrown when an embedded SQL resource cannot be found in any of the registered namespaces.
/// </summary>
public class EmbeddedSqlNotFoundException(string sqlFileName, IEnumerable<string?> namespaces)
    : NotFoundException($"Embedded SQL file '{sqlFileName}.sql' not found in namespaces: {string.Join(", ", namespaces)}")
{
    public string SqlFileName { get; } = sqlFileName;
}
