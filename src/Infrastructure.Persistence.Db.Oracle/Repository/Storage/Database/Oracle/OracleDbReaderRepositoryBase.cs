using Anarchy.Toolkit.Core.Entity;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Repository.Storage.Database.Oracle;

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
/// <param name="telemetry">Application telemetry used for logging and metrics.</param>
/// <example>
/// <code language="csharp">
/// public sealed class OrderReaderRepository(
///     [FromKeyedServices("reader")] IStoragePolicy&lt;OracleConnection&gt; dbPolicy, AppTelemetry telemetry)
///     : OracleDbReaderRepositoryBase&lt;PersistedOrder&gt;(dbPolicy, telemetry)
/// {
///     public override Task&lt;PersistedOrder&gt; GetByIdAsync(long id, CancellationToken? cancellationToken = null) =&gt;
///         GetByIdAsync("Order.Get.ById", id, cancellationToken);
///     // ...
/// }
/// </code>
/// </example>
public abstract class OracleDbReaderRepositoryBase<TPersistedEntity>(
    IStoragePolicy<OracleConnection> dbPolicy,
    AppTelemetry telemetry)
    : DbReaderRepositoryBase<OracleConnection, OracleDbDialect, TPersistedEntity>(dbPolicy, telemetry)
    where TPersistedEntity : IPersistedEntity;
