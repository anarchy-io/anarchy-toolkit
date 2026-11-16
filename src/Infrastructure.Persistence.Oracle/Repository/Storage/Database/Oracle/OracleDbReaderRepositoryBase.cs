using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Repository.Storage.Database.Oracle;

/// <summary>
/// Oracle flavour of <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.DbReaderRepositoryBase{TDbConnection, TDialect, TPersistedEntity}"/>
/// for read-side repositories backed by embedded SQL files.
/// </summary>
/// <remarks>
/// Inject the keyed <c>"reader"</c> storage policy (<c>[FromKeyedServices("reader")]</c>) to route queries to the
/// reader host; the unkeyed policy targets the writer.
/// </remarks>
/// <typeparam name="TPersistedEntity">The persisted entity type returned by the repository.</typeparam>
/// <param name="dbPolicy">Storage policy used to execute database operations (the writer policy when resolved unkeyed).</param>
/// <param name="observer">Observer used for logging and metrics.</param>
/// <example>
/// <code language="csharp">
/// public sealed class OrderReaderRepository(
///     [FromKeyedServices("reader")] IStoragePolicy&lt;OracleConnection&gt; dbPolicy, Observer observer)
///     : OracleDbReaderRepositoryBase&lt;PersistedOrder&gt;(dbPolicy, observer)
/// {
///     public override Task&lt;PersistedOrder&gt; GetByIdAsync(long id, CancellationToken? cancellationToken = null) =&gt;
///         GetByIdAsync("Order.Get.ById", id, cancellationToken);
///     // ...
/// }
/// </code>
/// </example>
public abstract class OracleDbReaderRepositoryBase<TPersistedEntity>(
    IStoragePolicy<OracleConnection> dbPolicy,
    Observer observer)
    : DbReaderRepositoryBase<OracleConnection, OracleDbDialect, TPersistedEntity>(dbPolicy, observer)
    where TPersistedEntity : IPersistedEntity;
