using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Anarchy.Toolkit.Testing.Db.Helper;
using Anarchy.Toolkit.Testing.Helper;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Testing.Oracle.Helper;

/// <summary>
///     Singleton-cached Oracle test fixtures. Reuses a single connection pool, retry policy builder and
///     resilience policy across tests, and exposes helpers to reset table state.
/// </summary>
public sealed class SingletonOracleHelper
{
    private static IDbConnectionPool<OracleConnection>? _instanceConnectionPool;
    private static IAsyncRetryPolicyBuilder<OracleConnection>? _instanceRetryPolicyBuilder;
    private static IStoragePolicy<OracleConnection>? _instanceDbPolicy;

    /// <summary>
    ///     Register a Dapper <see cref="DescriptionEnumTypeHandler{TEnum}" />
    ///     for the given enum type. Consumers call this for each domain enum they need to persist.
    /// </summary>
    public static void RegisterEnumTypeHandler<TEnum>() where TEnum : Enum
    {
        OracleHelper.RegisterEnumTypeHandler<TEnum>();
    }

    /// <summary>
    ///     Build a <see cref="OracleConnectionString" /> from the given settings.
    /// </summary>
    public static OracleConnectionString ConnectionStringFixture(IOracleAppSettings settings, bool forceRefresh = false)
    {
        return new OracleConnectionString(
            OracleOptionsMapper.ConnectionOptions(settings));
    }

    /// <summary>
    ///     Return the cached connection pool, optionally rebuilding it when <paramref name="forceRefresh" /> is set.
    /// </summary>
    public static IDbConnectionPool<OracleConnection> ConnectionPoolFixture(IOracleAppSettings settings, bool forceRefresh = false)
    {
        if (_instanceConnectionPool is not null && forceRefresh)
        {
            if (_instanceConnectionPool is IDisposable disposablePool)
            {
                disposablePool.Dispose();
            }
            else
            {
                _instanceConnectionPool.DestroyAsync().GetAwaiter().GetResult();
            }

            _instanceConnectionPool = null;
        }

        _instanceConnectionPool ??= new OracleConnectionPool(DataStorageInstanceType.Writer, ConnectionStringFixture(settings));
        return _instanceConnectionPool;
    }

    /// <summary>
    ///     Return the cached retry policy builder.
    /// </summary>
    public static IAsyncRetryPolicyBuilder<OracleConnection> RetryPolicyBuilder(IResilienceAppSettings settings, bool forceRefresh = false)
    {
        _instanceRetryPolicyBuilder ??= new OracleAsyncRetryPolicyBuilder(
            OracleOptionsMapper.ResilienceOptions(settings),
            InMemoryObserver.Create());
        return _instanceRetryPolicyBuilder;
    }

    /// <summary>
    ///     Return the cached resilience policy built from the test settings of <typeparamref name="TAppSettings" />,
    ///     optionally rebuilding it when <paramref name="forceRefresh" /> is set.
    /// </summary>
    public static IStoragePolicy<OracleConnection> DbPolicyFixture<TAppSettings>(bool forceRefresh = false)
        where TAppSettings : class, IOracleAppSettings, IResilienceAppSettings
    {
        return DbPolicyFixture(TestHelper.TestSettings<TAppSettings>(), forceRefresh);
    }

    /// <summary>
    ///     Return the cached resilience policy, optionally rebuilding it when <paramref name="forceRefresh" /> is set.
    ///     The settings must expose both the <c>Oracle</c> and <c>Resilience</c> sections.
    /// </summary>
    public static IStoragePolicy<OracleConnection> DbPolicyFixture<TSettings>(TSettings settings, bool forceRefresh = false)
        where TSettings : IOracleAppSettings, IResilienceAppSettings
    {
        if (_instanceDbPolicy is not null && forceRefresh)
        {
            _instanceDbPolicy = null;
        }

        _instanceDbPolicy ??= new OracleResilienceDbPolicy(
            RetryPolicyBuilder(settings, forceRefresh),
            ConnectionPoolFixture(settings, forceRefresh),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<OracleConnection>>(),
            InMemoryObserver.Create());

        return _instanceDbPolicy;
    }

    /// <summary>
    ///     Truncate the given table using the test settings of <typeparamref name="TAppSettings" />, with <c>TRUNCATE TABLE</c>
    ///     (identity columns are not restarted and dependent tables are not truncated).
    /// </summary>
    public static async Task TruncateTableAsync<TAppSettings>(string tableName,
                                                             CancellationToken cancellationToken = default)
        where TAppSettings : class, IOracleAppSettings, IResilienceAppSettings
    {
        await TruncateTableAsync(tableName, TestHelper.TestSettings<TAppSettings>(), cancellationToken);
    }

    /// <summary>
    ///     Truncate the given table using the given settings, with <c>TRUNCATE TABLE</c>
    ///     (identity columns are not restarted and dependent tables are not truncated).
    /// </summary>
    public static async Task TruncateTableAsync<TSettings>(string tableName, TSettings appSettings,
                                                          CancellationToken cancellationToken = default)
        where TSettings : IOracleAppSettings, IResilienceAppSettings
    {
        await DbTestHelper.TruncateTableAsync<OracleConnection, OracleDbDialect>(DbPolicyFixture(appSettings),
            tableName, cancellationToken);
    }

    /// <summary>
    ///     Delete all rows from the given table using the test settings of <typeparamref name="TAppSettings" />.
    /// </summary>
    public static async Task ClearTableAsync<TAppSettings>(string tableName,
                                                          CancellationToken cancellationToken = default)
        where TAppSettings : class, IOracleAppSettings, IResilienceAppSettings
    {
        await ClearTableAsync(tableName, TestHelper.TestSettings<TAppSettings>(), cancellationToken);
    }

    /// <summary>
    ///     Delete all rows from the given table using the given settings.
    /// </summary>
    public static async Task ClearTableAsync<TSettings>(string tableName, TSettings appSettings,
                                                       CancellationToken cancellationToken = default)
        where TSettings : IOracleAppSettings, IResilienceAppSettings
    {
        await DbTestHelper.ClearTableAsync(DbPolicyFixture(appSettings), tableName, cancellationToken);
    }
}
