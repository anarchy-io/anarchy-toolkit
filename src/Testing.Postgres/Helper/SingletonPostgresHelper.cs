using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;
using Anarchy.Toolkit.Testing.Db.Helper;
using Anarchy.Toolkit.Testing.Helper;
using Npgsql;

namespace Anarchy.Toolkit.Testing.Postgres.Helper;

/// <summary>
///     Singleton-cached Postgres test fixtures. Reuses a single connection pool, retry policy builder and
///     resilience policy across tests, and exposes helpers to reset table state.
/// </summary>
/// <remarks>
///     <para>
///     The instances are cached in static fields shared by every test in the process (without locking), and the
///     settings passed on later calls are ignored until <c>forceRefresh</c> is used. All fixtures target the writer host.
///     </para>
///     <para>
///     The retry policy builder is never rebuilt, and refreshing only the connection pool does not rebuild an
///     already cached policy; call <see cref="DbPolicyFixture{TSettings}(TSettings, bool, IAppTelemetry)" /> with
///     <c>forceRefresh</c> to rebuild both pool and policy.
///     </para>
/// </remarks>
/// <example>
///     <code language="csharp">
///     var dbPolicy = SingletonPostgresHelper.DbPolicyFixture&lt;MyTestSettings&gt;();
///     await SingletonPostgresHelper.TruncateTableAsync&lt;MyTestSettings&gt;("orders");
///     </code>
/// </example>
public sealed class SingletonPostgresHelper
{
    private static IDbConnectionPool<NpgsqlConnection>? _instanceConnectionPool;
    private static IAsyncRetryPolicyBuilder<NpgsqlConnection>? _instanceRetryPolicyBuilder;
    private static IStoragePolicy<NpgsqlConnection>? _instanceDbPolicy;

    /// <summary>
    ///     Register a Dapper <see cref="DescriptionEnumTypeHandler{TEnum}" />
    ///     for the given enum type. Consumers call this for each domain enum they need to persist.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to register.</typeparam>
    public static void RegisterEnumTypeHandler<TEnum>() where TEnum : Enum
    {
        PostgresHelper.RegisterEnumTypeHandler<TEnum>();
    }

    /// <summary>
    ///     Build a <see cref="PostgresConnectionString" /> from the given settings.
    /// </summary>
    /// <param name="settings">Settings exposing the <c>Postgres</c> section.</param>
    /// <param name="forceRefresh">Unused; the connection string is never cached.</param>
    /// <returns>A new connection string builder.</returns>
    public static PostgresConnectionString ConnectionStringFixture(IPostgresAppSettings settings, bool forceRefresh = false)
    {
        return new PostgresConnectionString(
            PostgresOptionsMapper.ConnectionOptions(settings));
    }

    /// <summary>
    ///     Return the cached connection pool, optionally rebuilding it when <paramref name="forceRefresh" /> is set.
    /// </summary>
    /// <param name="settings">Settings used when the pool has to be created.</param>
    /// <param name="forceRefresh">When <c>true</c>, disposes the cached pool and creates a new one.</param>
    /// <returns>The cached connection pool.</returns>
    public static IDbConnectionPool<NpgsqlConnection> ConnectionPoolFixture(IPostgresAppSettings settings, bool forceRefresh = false)
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

        _instanceConnectionPool ??= new PostgresConnectionPool(DataStorageInstanceType.Writer, ConnectionStringFixture(settings));
        return _instanceConnectionPool;
    }

    /// <summary>
    ///     Return the cached retry policy builder, creating it on first use.
    /// </summary>
    /// <param name="settings">Settings used when the builder has to be created.</param>
    /// <param name="forceRefresh">Currently ignored: the builder is never rebuilt.</param>
    /// <param name="telemetry">Optional application telemetry for retry logging.</param>
    /// <returns>The cached retry policy builder.</returns>
    public static IAsyncRetryPolicyBuilder<NpgsqlConnection> RetryPolicyBuilder(IResilienceAppSettings settings, bool forceRefresh = false,
        IAppTelemetry? telemetry = null)
    {
        _instanceRetryPolicyBuilder ??= new PostgresAsyncRetryPolicyBuilder(
            PostgresOptionsMapper.ResilienceOptions(settings),
            telemetry);
        return _instanceRetryPolicyBuilder;
    }

    /// <summary>
    ///     Return the cached resilience policy built from the test settings of <typeparamref name="TAppSettings" />,
    ///     optionally rebuilding it when <paramref name="forceRefresh" /> is set.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <param name="forceRefresh">When <c>true</c>, rebuilds the policy and the connection pool.</param>
    /// <param name="telemetry">Optional application telemetry for the policy and retry builder.</param>
    /// <returns>The cached policy.</returns>
    public static IStoragePolicy<NpgsqlConnection> DbPolicyFixture<TAppSettings>(bool forceRefresh = false, IAppTelemetry? telemetry = null)
        where TAppSettings : class, IPostgresAppSettings, IResilienceAppSettings
    {
        return DbPolicyFixture(TestHelper.TestSettings<TAppSettings>(), forceRefresh, telemetry);
    }

    /// <summary>
    ///     Return the cached resilience policy, optionally rebuilding it when <paramref name="forceRefresh" /> is set.
    ///     The settings must expose both the <c>Postgres</c> and <c>Resilience</c> sections.
    /// </summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    /// <param name="settings">Settings used when the policy has to be created.</param>
    /// <param name="forceRefresh">When <c>true</c>, rebuilds the policy and the connection pool.</param>
    /// <param name="telemetry">Optional application telemetry for the policy and retry builder.</param>
    /// <returns>The cached policy.</returns>
    public static IStoragePolicy<NpgsqlConnection> DbPolicyFixture<TSettings>(TSettings settings, bool forceRefresh = false,
        IAppTelemetry? telemetry = null)
        where TSettings : IPostgresAppSettings, IResilienceAppSettings
    {
        if (_instanceDbPolicy is not null && forceRefresh)
        {
            _instanceDbPolicy = null;
        }

        _instanceDbPolicy ??= new PostgresResilienceDbPolicy(
            RetryPolicyBuilder(settings, forceRefresh, telemetry),
            ConnectionPoolFixture(settings, forceRefresh),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<NpgsqlConnection>>(),
            telemetry);

        return _instanceDbPolicy;
    }

    /// <summary>
    ///     Truncate the given table using the test settings of <typeparamref name="TAppSettings" />, restarting
    ///     identity and cascading to dependents.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the table has been truncated.</returns>
    public static async Task TruncateTableAsync<TAppSettings>(string tableName,
                                                             CancellationToken cancellationToken = default)
        where TAppSettings : class, IPostgresAppSettings, IResilienceAppSettings
    {
        await TruncateTableAsync(tableName, TestHelper.TestSettings<TAppSettings>(), cancellationToken);
    }

    /// <summary>
    ///     Truncate the given table using the given settings, restarting identity and cascading to dependents.
    /// </summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="appSettings">Settings used to obtain the cached policy.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the table has been truncated.</returns>
    public static async Task TruncateTableAsync<TSettings>(string tableName, TSettings appSettings,
                                                          CancellationToken cancellationToken = default)
        where TSettings : IPostgresAppSettings, IResilienceAppSettings
    {
        await DbTestHelper.TruncateTableAsync<NpgsqlConnection, PostgresDbDialect>(DbPolicyFixture(appSettings),
            tableName, cancellationToken);
    }

    /// <summary>
    ///     Delete all rows from the given table using the test settings of <typeparamref name="TAppSettings" />.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the table has been cleared.</returns>
    public static async Task ClearTableAsync<TAppSettings>(string tableName,
                                                          CancellationToken cancellationToken = default)
        where TAppSettings : class, IPostgresAppSettings, IResilienceAppSettings
    {
        await ClearTableAsync(tableName, TestHelper.TestSettings<TAppSettings>(), cancellationToken);
    }

    /// <summary>
    ///     Delete all rows from the given table using the given settings.
    /// </summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    /// <param name="tableName">Name of the table.</param>
    /// <param name="appSettings">Settings used to obtain the cached policy.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the table has been cleared.</returns>
    public static async Task ClearTableAsync<TSettings>(string tableName, TSettings appSettings,
                                                       CancellationToken cancellationToken = default)
        where TSettings : IPostgresAppSettings, IResilienceAppSettings
    {
        await DbTestHelper.ClearTableAsync(DbPolicyFixture(appSettings), tableName, cancellationToken);
    }
}
