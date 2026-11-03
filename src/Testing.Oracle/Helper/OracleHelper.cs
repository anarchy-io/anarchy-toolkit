using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Anarchy.Toolkit.Testing.Db.Helper;
using Anarchy.Toolkit.Testing.Helper;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Testing.Oracle.Helper;

/// <summary>
///     Test fixtures that build Oracle persistence concretes from the application settings (the test settings of
///     the consumer's settings type, or any settings exposing the required sections).
/// </summary>
public sealed class OracleHelper
{
    /// <summary>
    ///     Register a Dapper <see cref="DescriptionEnumTypeHandler{TEnum}" /> for the given enum type so that
    ///     its values are mapped to/from their <see cref="System.ComponentModel.DescriptionAttribute" /> string.
    ///     Consumers call this for each domain enum they need to persist.
    /// </summary>
    public static void RegisterEnumTypeHandler<TEnum>() where TEnum : Enum
    {
        DbTestHelper.RegisterEnumTypeHandler<TEnum>();
    }

    /// <summary>
    ///     Build a <see cref="OracleResilienceDbPolicy" /> fixture from the test settings of
    ///     <typeparamref name="TAppSettings" />.
    /// </summary>
    public static OracleResilienceDbPolicy DbPolicyFixture<TAppSettings>()
        where TAppSettings : class, IOracleAppSettings, IResilienceAppSettings
    {
        return DbPolicyFixture(TestHelper.TestSettings<TAppSettings>());
    }

    /// <summary>
    ///     Build a <see cref="OracleResilienceDbPolicy" /> fixture from the given settings, which must expose both
    ///     the <c>Oracle</c> and <c>Resilience</c> sections.
    /// </summary>
    public static OracleResilienceDbPolicy DbPolicyFixture<TSettings>(TSettings appSettings)
        where TSettings : IOracleAppSettings, IResilienceAppSettings
    {
        var connectionPool = new OracleConnectionPool(
            DataStorageInstanceType.Writer,
            new OracleConnectionString(OracleOptionsMapper.ConnectionOptions(appSettings)));

        var retryPolicyBuilder = new OracleAsyncRetryPolicyBuilder(
            OracleOptionsMapper.ResilienceOptions(appSettings),
            InMemoryObserver.Create());

        return new OracleResilienceDbPolicy(
            retryPolicyBuilder,
            connectionPool,
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<OracleConnection>>(),
            InMemoryObserver.Create());
    }

    /// <summary>
    ///     Build a <see cref="OracleDbUnitOfWork" /> fixture from the test settings of
    ///     <typeparamref name="TAppSettings" />.
    /// </summary>
    public static OracleDbUnitOfWork DbUnitOfWorkFixture<TAppSettings>()
        where TAppSettings : class, IOracleAppSettings
    {
        return DbUnitOfWorkFixture(TestHelper.TestSettings<TAppSettings>());
    }

    /// <summary>
    ///     Build a <see cref="OracleDbUnitOfWork" /> fixture from the given settings.
    /// </summary>
    public static OracleDbUnitOfWork DbUnitOfWorkFixture(IOracleAppSettings appSettings)
    {
        return new OracleDbUnitOfWork(
            new OracleConnectionPool(
                DataStorageInstanceType.Writer,
                new OracleConnectionString(OracleOptionsMapper.ConnectionOptions(appSettings))),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<OracleConnection>>(),
            InMemoryObserver.Create());
    }
}
