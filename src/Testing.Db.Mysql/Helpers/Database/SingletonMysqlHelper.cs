using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Testing.Db.Helpers.Database;
using Anarchy.Toolkit.Testing.Db.Mysql.Entity.Configuration;
using Anarchy.Toolkit.Testing.Helpers.Tests;
using MySqlConnector;

namespace Anarchy.Toolkit.Testing.Db.Mysql.Helpers.Database;

/// <summary>Cached MySQL fixtures and table cleanup helpers for integration tests.</summary>
/// <remarks>
///     <para>
///     The connection pool, retry policy builder and resilience policy are cached in static fields (guarded by a
///     lock) and shared by every test in the process; the settings passed on later calls are ignored until
///     <c>forceRefresh</c> is used. Refreshing the pool or the retry builder also drops the cached policy.
///     </para>
///     <para>All fixtures target the writer host.</para>
/// </remarks>
/// <example>
///     <code language="csharp">
///     var dbPolicy = SingletonMysqlHelper.DbPolicyFixture&lt;MyTestSettings&gt;();
///     await SingletonMysqlHelper.ClearTableAsync&lt;MyTestSettings&gt;("orders");
///     </code>
/// </example>
public sealed class SingletonMysqlHelper
{
    private static IDbConnectionPool<MySqlConnection>? _connectionPool;
    private static IAsyncRetryPolicyBuilder<MySqlConnection>? _retryPolicyBuilder;
    private static IStoragePolicy<MySqlConnection>? _dbPolicy;
    private static readonly object Sync = new();

    /// <summary>
    ///     Register a Dapper <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler.DescriptionEnumTypeHandler{TEnum}" />
    ///     for the given enum type. Consumers call this for each domain enum they need to persist.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to register.</typeparam>
    public static void RegisterEnumTypeHandler<TEnum>() where TEnum : Enum => MysqlHelper.RegisterEnumTypeHandler<TEnum>();

    /// <summary>
    ///     Build a <see cref="MysqlConnectionString" /> from the given settings. Not cached.
    /// </summary>
    /// <param name="settings">Settings exposing the <c>Mysql</c> section.</param>
    /// <param name="forceRefresh">Unused; kept for signature symmetry with the other fixtures.</param>
    /// <returns>A new connection string builder.</returns>
    public static MysqlConnectionString ConnectionStringFixture(IMysqlAppSettings settings, bool forceRefresh = false) =>
        new(MysqlOptionsMapper.ConnectionOptions(settings));

    /// <summary>
    ///     Return the cached writer connection pool, creating it on first use.
    /// </summary>
    /// <param name="settings">Settings used when the pool has to be created.</param>
    /// <param name="forceRefresh">
    ///     When <c>true</c>, disposes the cached pool (and drops the cached policy) and creates a new one.
    /// </param>
    /// <returns>The cached connection pool.</returns>
    public static IDbConnectionPool<MySqlConnection> ConnectionPoolFixture(IMysqlAppSettings settings, bool forceRefresh = false)
    {
        lock (Sync)
        {
            if (forceRefresh && _connectionPool is not null)
            {
                if (_connectionPool is IDisposable disposable) disposable.Dispose();
                else _connectionPool.DestroyAsync().GetAwaiter().GetResult();
                _connectionPool = null;
                _dbPolicy = null;
            }
            return _connectionPool ??= new MysqlConnectionPool(DataStorageInstanceType.Writer, ConnectionStringFixture(settings));
        }
    }

    /// <summary>
    ///     Return the cached retry policy builder, creating it on first use.
    /// </summary>
    /// <param name="settings">Settings used when the builder has to be created.</param>
    /// <param name="forceRefresh">When <c>true</c>, drops the cached builder (and policy) and creates a new one.</param>
    /// <param name="telemetry">Optional application telemetry for retry logging.</param>
    /// <returns>The cached retry policy builder.</returns>
    public static IAsyncRetryPolicyBuilder<MySqlConnection> RetryPolicyBuilder(IResilienceAppSettings settings, bool forceRefresh = false,
        AppTelemetry? telemetry = null)
    {
        lock (Sync)
        {
            if (forceRefresh)
            {
                _retryPolicyBuilder = null;
                _dbPolicy = null;
            }
            return _retryPolicyBuilder ??= new MysqlAsyncRetryPolicyBuilder(MysqlOptionsMapper.ResilienceOptions(settings), telemetry);
        }
    }

    /// <summary>
    ///     Return the cached resilience policy built from the test settings of <typeparamref name="TAppSettings" />.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <param name="forceRefresh">When <c>true</c>, rebuilds the policy, the pool and the retry builder.</param>
    /// <param name="telemetry">Optional application telemetry for the policy and retry builder.</param>
    /// <returns>The cached policy.</returns>
    public static IStoragePolicy<MySqlConnection> DbPolicyFixture<TAppSettings>(bool forceRefresh = false, AppTelemetry? telemetry = null)
        where TAppSettings : class, IMysqlAppSettings, IResilienceAppSettings =>
        DbPolicyFixture(TestHelper.TestSettings<TAppSettings>(), forceRefresh, telemetry);

    /// <summary>
    ///     Return the cached resilience policy, creating it on first use. The settings must expose both the
    ///     <c>Mysql</c> and <c>Resilience</c> sections.
    /// </summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    /// <param name="settings">Settings used when the policy has to be created.</param>
    /// <param name="forceRefresh">When <c>true</c>, rebuilds the policy, the pool and the retry builder.</param>
    /// <param name="telemetry">Optional application telemetry for the policy and retry builder.</param>
    /// <returns>The cached policy.</returns>
    public static IStoragePolicy<MySqlConnection> DbPolicyFixture<TSettings>(TSettings settings, bool forceRefresh = false,
        AppTelemetry? telemetry = null)
        where TSettings : IMysqlAppSettings, IResilienceAppSettings
    {
        lock (Sync)
        {
            if (forceRefresh) _dbPolicy = null;
            return _dbPolicy ??= new MysqlResilienceDbPolicy(RetryPolicyBuilder(settings, forceRefresh, telemetry),
                ConnectionPoolFixture(settings, forceRefresh), new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>>(),
                telemetry);
        }
    }

    /// <summary>Truncates the table, resetting AUTO_INCREMENT. MySQL may reject tables referenced by foreign keys.</summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <param name="tableName">Name of the table to truncate.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the table has been truncated.</returns>
    public static Task TruncateTableAsync<TAppSettings>(string tableName, CancellationToken cancellationToken = default)
        where TAppSettings : class, IMysqlAppSettings, IResilienceAppSettings =>
        TruncateTableAsync(tableName, TestHelper.TestSettings<TAppSettings>(), cancellationToken);

    /// <summary>Truncates the table using the given settings, resetting AUTO_INCREMENT.</summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    /// <param name="tableName">Name of the table to truncate.</param>
    /// <param name="settings">Settings used to obtain the cached policy.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the table has been truncated.</returns>
    public static Task TruncateTableAsync<TSettings>(string tableName, TSettings settings, CancellationToken cancellationToken = default)
        where TSettings : IMysqlAppSettings, IResilienceAppSettings =>
        DbTestHelper.TruncateTableAsync<MySqlConnection, MysqlDbDialect>(DbPolicyFixture(settings), tableName, cancellationToken);

    /// <summary>Deletes all rows from the table using the test settings of <typeparamref name="TAppSettings" />.</summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <param name="tableName">Name of the table to clear.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the rows have been deleted.</returns>
    public static Task ClearTableAsync<TAppSettings>(string tableName, CancellationToken cancellationToken = default)
        where TAppSettings : class, IMysqlAppSettings, IResilienceAppSettings =>
        ClearTableAsync(tableName, TestHelper.TestSettings<TAppSettings>(), cancellationToken);

    /// <summary>Deletes all rows from the table using the given settings.</summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    /// <param name="tableName">Name of the table to clear.</param>
    /// <param name="settings">Settings used to obtain the cached policy.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the rows have been deleted.</returns>
    public static Task ClearTableAsync<TSettings>(string tableName, TSettings settings, CancellationToken cancellationToken = default)
        where TSettings : IMysqlAppSettings, IResilienceAppSettings =>
        DbTestHelper.ClearTableAsync(DbPolicyFixture(settings), tableName, cancellationToken);
}
