using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Service.Storage.Database.Oracle;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;
using Anarchy.Toolkit.Testing.Db.Helper;
using Anarchy.Toolkit.Testing.Helper;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Testing.Db.Oracle.Helper;

/// <summary>
///     Test fixtures that build Oracle persistence concretes from the application settings (the test settings of
///     the consumer's settings type, or any settings exposing the required sections).
/// </summary>
/// <remarks>
///     Every call builds new instances (connection pool included), targeting the writer host. Use
///     <see cref="SingletonOracleHelper" /> to share a cached pool and policy across tests.
/// </remarks>
/// <example>
///     <code language="csharp">
///     OracleHelper.RegisterEnumTypeHandler&lt;OrderStatus&gt;();
///     var dbPolicy = OracleHelper.DbPolicyFixture&lt;MyTestSettings&gt;();
///     var unitOfWork = OracleHelper.DbUnitOfWorkFixture&lt;MyTestSettings&gt;();
///     </code>
/// </example>
public sealed class OracleHelper
{
    /// <summary>
    ///     Register a Dapper <see cref="DescriptionEnumTypeHandler{TEnum}" /> for the given enum type so that
    ///     its values are mapped to/from their <see cref="System.ComponentModel.DescriptionAttribute" /> string.
    ///     Consumers call this for each domain enum they need to persist.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to register.</typeparam>
    public static void RegisterEnumTypeHandler<TEnum>() where TEnum : Enum
    {
        DbTestHelper.RegisterEnumTypeHandler<TEnum>();
    }

    /// <summary>
    ///     Build a <see cref="OracleResilienceDbPolicy" /> fixture from the test settings of
    ///     <typeparamref name="TAppSettings" />.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <returns>A new resilience policy over a new writer connection pool.</returns>
    public static OracleResilienceDbPolicy DbPolicyFixture<TAppSettings>(AppTelemetry? telemetry = null)
        where TAppSettings : class, IOracleAppSettings, IResilienceAppSettings
    {
        return DbPolicyFixture(TestHelper.TestSettings<TAppSettings>(), telemetry);
    }

    /// <summary>
    ///     Build a <see cref="OracleResilienceDbPolicy" /> fixture from the given settings, which must expose both
    ///     the <c>Oracle</c> and <c>Resilience</c> sections.
    /// </summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    /// <param name="appSettings">The settings to build the fixture from.</param>
    /// <param name="telemetry">Optional application telemetry for the policy.</param>
    /// <returns>A new resilience policy over a new writer connection pool.</returns>
    public static OracleResilienceDbPolicy DbPolicyFixture<TSettings>(TSettings appSettings, AppTelemetry? telemetry = null)
        where TSettings : IOracleAppSettings, IResilienceAppSettings
    {
        var connectionPool = new OracleConnectionPool(
            DataStorageInstanceType.Writer,
            new OracleConnectionString(OracleOptionsMapper.ConnectionOptions(appSettings)));

        var retryPolicyBuilder = new OracleAsyncRetryPolicyBuilder(
            OracleOptionsMapper.ResilienceOptions(appSettings),
            telemetry);

        return new OracleResilienceDbPolicy(
            retryPolicyBuilder,
            connectionPool,
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<OracleConnection>>(),
            telemetry);
    }

    /// <summary>
    ///     Build a <see cref="OracleDbUnitOfWork" /> fixture from the test settings of
    ///     <typeparamref name="TAppSettings" />.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <param name="telemetry">Optional application telemetry for the unit of work.</param>
    /// <returns>A new unit of work over a new writer connection pool.</returns>
    public static OracleDbUnitOfWork DbUnitOfWorkFixture<TAppSettings>(AppTelemetry? telemetry = null)
        where TAppSettings : class, IOracleAppSettings
    {
        return DbUnitOfWorkFixture(TestHelper.TestSettings<TAppSettings>(), telemetry);
    }

    /// <summary>
    ///     Build a <see cref="OracleDbUnitOfWork" /> fixture from the given settings.
    /// </summary>
    /// <param name="appSettings">The settings to build the fixture from.</param>
    /// <param name="telemetry">Optional application telemetry for the unit of work.</param>
    /// <returns>A new unit of work over a new writer connection pool.</returns>
    /// <remarks>
    ///     The unit of work uses its own async flow context, so repositories built from a separate
    ///     <see cref="DbPolicyFixture{TSettings}(TSettings, AppTelemetry)" /> do not join its sessions.
    /// </remarks>
    public static OracleDbUnitOfWork DbUnitOfWorkFixture(IOracleAppSettings appSettings, AppTelemetry? telemetry = null)
    {
        return new OracleDbUnitOfWork(
            new OracleConnectionPool(
                DataStorageInstanceType.Writer,
                new OracleConnectionString(OracleOptionsMapper.ConnectionOptions(appSettings))),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<OracleConnection>>(),
            telemetry);
    }
}
