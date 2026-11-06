using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;
using Anarchy.Toolkit.Testing.Db.Helper;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Testing.Mysql.Entity.Configuration;
using MySqlConnector;

namespace Anarchy.Toolkit.Testing.Mysql.Helper;

/// <summary>Cached MySQL fixtures and table cleanup helpers for integration tests.</summary>
public sealed class SingletonMysqlHelper
{
    private static IDbConnectionPool<MySqlConnection>? _connectionPool;
    private static IAsyncRetryPolicyBuilder<MySqlConnection>? _retryPolicyBuilder;
    private static IStoragePolicy<MySqlConnection>? _dbPolicy;
    private static readonly object Sync = new();

    public static void RegisterEnumTypeHandler<TEnum>() where TEnum : Enum => MysqlHelper.RegisterEnumTypeHandler<TEnum>();

    public static MysqlConnectionString ConnectionStringFixture(IMysqlAppSettings settings, bool forceRefresh = false) =>
        new(MysqlOptionsMapper.ConnectionOptions(settings));

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

    public static IAsyncRetryPolicyBuilder<MySqlConnection> RetryPolicyBuilder(IResilienceAppSettings settings, bool forceRefresh = false)
    {
        lock (Sync)
        {
            if (forceRefresh)
            {
                _retryPolicyBuilder = null;
                _dbPolicy = null;
            }
            return _retryPolicyBuilder ??= new MysqlAsyncRetryPolicyBuilder(MysqlOptionsMapper.ResilienceOptions(settings), InMemoryObserver.Create());
        }
    }

    public static IStoragePolicy<MySqlConnection> DbPolicyFixture<TAppSettings>(bool forceRefresh = false)
        where TAppSettings : class, IMysqlAppSettings, IResilienceAppSettings =>
        DbPolicyFixture(TestHelper.TestSettings<TAppSettings>(), forceRefresh);

    public static IStoragePolicy<MySqlConnection> DbPolicyFixture<TSettings>(TSettings settings, bool forceRefresh = false)
        where TSettings : IMysqlAppSettings, IResilienceAppSettings
    {
        lock (Sync)
        {
            if (forceRefresh) _dbPolicy = null;
            return _dbPolicy ??= new MysqlResilienceDbPolicy(RetryPolicyBuilder(settings, forceRefresh),
                ConnectionPoolFixture(settings, forceRefresh), new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>>(),
                InMemoryObserver.Create());
        }
    }

    /// <summary>Truncates the table, resetting AUTO_INCREMENT. MySQL may reject tables referenced by foreign keys.</summary>
    public static Task TruncateTableAsync<TAppSettings>(string tableName, CancellationToken cancellationToken = default)
        where TAppSettings : class, IMysqlAppSettings, IResilienceAppSettings =>
        TruncateTableAsync(tableName, TestHelper.TestSettings<TAppSettings>(), cancellationToken);

    public static Task TruncateTableAsync<TSettings>(string tableName, TSettings settings, CancellationToken cancellationToken = default)
        where TSettings : IMysqlAppSettings, IResilienceAppSettings =>
        DbTestHelper.TruncateTableAsync<MySqlConnection, MysqlDbDialect>(DbPolicyFixture(settings), tableName, cancellationToken);

    public static Task ClearTableAsync<TAppSettings>(string tableName, CancellationToken cancellationToken = default)
        where TAppSettings : class, IMysqlAppSettings, IResilienceAppSettings =>
        ClearTableAsync(tableName, TestHelper.TestSettings<TAppSettings>(), cancellationToken);

    public static Task ClearTableAsync<TSettings>(string tableName, TSettings settings, CancellationToken cancellationToken = default)
        where TSettings : IMysqlAppSettings, IResilienceAppSettings =>
        DbTestHelper.ClearTableAsync(DbPolicyFixture(settings), tableName, cancellationToken);
}
