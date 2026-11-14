using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;
using Anarchy.Toolkit.Testing.Db.Helper;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Testing.Mysql.Entity.Configuration;
using MySqlConnector;

namespace Anarchy.Toolkit.Testing.Mysql.Helper;

/// <summary>Creates MySQL persistence fixtures from application settings.</summary>
public sealed class MysqlHelper
{
    public static void RegisterEnumTypeHandler<TEnum>() where TEnum : Enum => DbTestHelper.RegisterEnumTypeHandler<TEnum>();

    public static MysqlResilienceDbPolicy DbPolicyFixture<TAppSettings>()
        where TAppSettings : class, IMysqlAppSettings, IResilienceAppSettings => DbPolicyFixture(TestHelper.TestSettings<TAppSettings>());

    public static MysqlResilienceDbPolicy DbPolicyFixture<TSettings>(TSettings settings)
        where TSettings : IMysqlAppSettings, IResilienceAppSettings =>
        new(new MysqlAsyncRetryPolicyBuilder(MysqlOptionsMapper.ResilienceOptions(settings), InMemoryObserver.Create()),
            new MysqlConnectionPool(DataStorageInstanceType.Writer, new MysqlConnectionString(MysqlOptionsMapper.ConnectionOptions(settings))),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>>(), InMemoryObserver.Create());

    public static MysqlDbUnitOfWork DbUnitOfWorkFixture<TAppSettings>() where TAppSettings : class, IMysqlAppSettings =>
        DbUnitOfWorkFixture(TestHelper.TestSettings<TAppSettings>());

    public static MysqlDbUnitOfWork DbUnitOfWorkFixture(IMysqlAppSettings settings) =>
        new(new MysqlConnectionPool(DataStorageInstanceType.Writer, new MysqlConnectionString(MysqlOptionsMapper.ConnectionOptions(settings))),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>>(), InMemoryObserver.Create());
}
