using Anarchy.Toolkit.Core.Exceptions;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Exceptions;

/// <summary>
/// Thrown when an embedded SQL resource cannot be found in any of the registered namespaces.
/// </summary>
/// <param name="sqlFileName">The requested query name, without the <c>.sql</c> extension.</param>
/// <param name="namespaces">The namespaces that were searched, listed in the exception message.</param>
/// <remarks>
/// Raised by <see cref="DbRepositoryBase{TDbConnection}.LoadSql"/>. Usually means the file is missing, misnamed, or not
/// marked as <c>EmbeddedResource</c> in the project file.
/// </remarks>
public class EmbeddedSqlNotFoundException(string sqlFileName, IEnumerable<string?> namespaces)
    : NotFoundException($"Embedded SQL file '{sqlFileName}.sql' not found in namespaces: {string.Join(", ", namespaces)}")
{
    /// <summary>The requested query name, without the <c>.sql</c> extension.</summary>
    public string SqlFileName { get; } = sqlFileName;
}
