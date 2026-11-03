using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.Exceptions;
using System.Collections.Concurrent;
using System.Reflection;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;

/// <summary>
/// Provides a base abstraction for database repositories that rely on embedded SQL files
/// and a pluggable <see cref="IStoragePolicy{TDbConnection}"/> for resilience and connection management.
/// <para>
/// This class automatically discovers SQL resources embedded under <c>Queries</c> folders in the
/// namespaces of all types in the inheritance chain (from the most derived repository up to
/// <see cref="DbRepositoryBase{TDbConnection}"/> itself). This allows child repositories to override
/// queries defined by base repositories simply by providing a query file with the same name in their own namespace.
/// </para>
/// <para>
/// Embedded SQL files must be marked as <c>&lt;EmbeddedResource Include="**\Queries\**\*.sql"/&gt;</c>
/// in the consumer's project file. Subfolders in <c>Queries</c> are mapped to dotted paths. For example:
/// <c>Queries/Distributor/GetById.sql</c> becomes <c>.Queries.Distributor.GetById.sql</c>.
/// Resolution happens per assembly, so a consumer's SQL resources resolve within their own assembly.
/// </para>
/// </summary>
/// <typeparam name="TDbConnection">The type of database connection (e.g. <c>NpgsqlConnection</c>).</typeparam>
public abstract class DbRepositoryBase<TDbConnection>
{
    private static readonly ConcurrentDictionary<string, string> QueryCache = new();

    private readonly IReadOnlyCollection<(string Namespace, Assembly Assembly)> _namespaceAssemblies;

    protected readonly IStoragePolicy<TDbConnection> DbPolicy;
    protected readonly Observer Observer;

    protected DbRepositoryBase(IStoragePolicy<TDbConnection> dbPolicy, Observer observer)
    {
        DbPolicy = dbPolicy;
        Observer = observer;

        _namespaceAssemblies = GetInheritanceChain(GetType())
            .Select(t => (t.Namespace!, t.Assembly))
            .Distinct()
            .ToArray();
    }

    private static IEnumerable<Type> GetInheritanceChain(Type? type)
    {
        while (type != null && type != typeof(object))
        {
            yield return type;
            type = type.BaseType!;
        }
    }

    /// <summary>
    /// Loads the contents of an embedded SQL file, using the repository’s namespace hierarchy
    /// to resolve the query. The search order starts with the namespace of the most derived repository,
    /// then falls back through each base class namespace in turn.
    /// </summary>
    /// <param name="sqlFileName">
    /// The relative query path without extension. Dots represent subfolders.
    /// For example, calling <c>LoadSql("Common.MyQuery")</c> looks for an embedded resource named
    /// <c>Namespace.Queries.Common.MyQuery.sql</c>.
    /// </param>
    /// <returns>The full text of the SQL file as a string.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the SQL file cannot be found in any of the applicable namespaces.
    /// </exception>
    protected string LoadSql(string sqlFileName)
    {
        return QueryCache.GetOrAdd(sqlFileName, key => {
            foreach (var (ns, assembly) in _namespaceAssemblies)
            {
                var resourceName = $"{ns}.Queries.{key}.sql";
                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }

            throw new EmbeddedSqlNotFoundException(sqlFileName, _namespaceAssemblies.Select(x => x.Namespace));
        });
    }
}
