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
/// <remarks>
///     Every call builds new instances (connection pool included), targeting the writer host. Use
///     <see cref="SingletonMysqlHelper" /> to share a cached pool and policy across tests.
/// </remarks>
/// <example>
///     <code language="csharp">
///     var dbPolicy = MysqlHelper.DbPolicyFixture&lt;MyTestSettings&gt;();
///     var unitOfWork = MysqlHelper.DbUnitOfWorkFixture&lt;MyTestSettings&gt;();
///     </code>
/// </example>
public sealed class MysqlHelper
{
    /// <summary>
    ///     Register a Dapper <see cref="Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler.DescriptionEnumTypeHandler{TEnum}" />
    ///     for the given enum type. Consumers call this for each domain enum they need to persist.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to register.</typeparam>
    public static void RegisterEnumTypeHandler<TEnum>() where TEnum : Enum => DbTestHelper.RegisterEnumTypeHandler<TEnum>();

    /// <summary>
    ///     Build a <see cref="MysqlResilienceDbPolicy" /> fixture from the test settings of
    ///     <typeparamref name="TAppSettings" />.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <returns>A new resilience policy over a new writer connection pool.</returns>
    public static MysqlResilienceDbPolicy DbPolicyFixture<TAppSettings>()
        where TAppSettings : class, IMysqlAppSettings, IResilienceAppSettings => DbPolicyFixture(TestHelper.TestSettings<TAppSettings>());

    /// <summary>
    ///     Build a <see cref="MysqlResilienceDbPolicy" /> fixture from the given settings, which must expose both
    ///     the <c>Mysql</c> and <c>Resilience</c> sections.
    /// </summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    /// <param name="settings">The settings to build the fixture from.</param>
    /// <returns>A new resilience policy over a new writer connection pool.</returns>
    public static MysqlResilienceDbPolicy DbPolicyFixture<TSettings>(TSettings settings)
        where TSettings : IMysqlAppSettings, IResilienceAppSettings =>
        new(new MysqlAsyncRetryPolicyBuilder(MysqlOptionsMapper.ResilienceOptions(settings), InMemoryObserver.Create()),
            new MysqlConnectionPool(DataStorageInstanceType.Writer, new MysqlConnectionString(MysqlOptionsMapper.ConnectionOptions(settings))),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>>(), InMemoryObserver.Create());

    /// <summary>
    ///     Build a <see cref="MysqlDbUnitOfWork" /> fixture from the test settings of
    ///     <typeparamref name="TAppSettings" />.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <returns>A new unit of work over a new writer connection pool.</returns>
    public static MysqlDbUnitOfWork DbUnitOfWorkFixture<TAppSettings>() where TAppSettings : class, IMysqlAppSettings =>
        DbUnitOfWorkFixture(TestHelper.TestSettings<TAppSettings>());

    /// <summary>
    ///     Build a <see cref="MysqlDbUnitOfWork" /> fixture from the given settings.
    /// </summary>
    /// <param name="settings">The settings to build the fixture from.</param>
    /// <returns>A new unit of work over a new writer connection pool.</returns>
    /// <remarks>
    ///     The unit of work uses its own async flow context, so repositories built from a separate
    ///     <see cref="DbPolicyFixture{TSettings}(TSettings)" /> do not join its sessions.
    /// </remarks>
    public static MysqlDbUnitOfWork DbUnitOfWorkFixture(IMysqlAppSettings settings) =>
        new(new MysqlConnectionPool(DataStorageInstanceType.Writer, new MysqlConnectionString(MysqlOptionsMapper.ConnectionOptions(settings))),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>>(), InMemoryObserver.Create());
}
