using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Repository.Storage.Database.TypeHandler;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;
using Anarchy.Toolkit.Testing.Db.Helper;
using Anarchy.Toolkit.Testing.Helper;
using Npgsql;

namespace Anarchy.Toolkit.Testing.Postgres.Helper;

/// <summary>
///     Test fixtures that build Postgres persistence concretes from the application settings (the test settings of
///     the consumer's settings type, or any settings exposing the required sections).
/// </summary>
/// <remarks>
///     Every call builds new instances (connection pool included), targeting the writer host. Use
///     <see cref="SingletonPostgresHelper" /> to share a cached pool and policy across tests.
/// </remarks>
/// <example>
///     <code language="csharp">
///     PostgresHelper.RegisterEnumTypeHandler&lt;OrderStatus&gt;();
///     var dbPolicy = PostgresHelper.DbPolicyFixture&lt;MyTestSettings&gt;();
///     var unitOfWork = PostgresHelper.DbUnitOfWorkFixture&lt;MyTestSettings&gt;();
///     </code>
/// </example>
public sealed class PostgresHelper
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
    ///     Build a <see cref="PostgresResilienceDbPolicy" /> fixture from the test settings of
    ///     <typeparamref name="TAppSettings" />.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <returns>A new resilience policy over a new writer connection pool.</returns>
    public static PostgresResilienceDbPolicy DbPolicyFixture<TAppSettings>(IAppTelemetry? telemetry = null)
        where TAppSettings : class, IPostgresAppSettings, IResilienceAppSettings
    {
        return DbPolicyFixture(TestHelper.TestSettings<TAppSettings>(), telemetry);
    }

    /// <summary>
    ///     Build a <see cref="PostgresResilienceDbPolicy" /> fixture from the given settings, which must expose both
    ///     the <c>Postgres</c> and <c>Resilience</c> sections.
    /// </summary>
    /// <typeparam name="TSettings">The settings type.</typeparam>
    /// <param name="appSettings">The settings to build the fixture from.</param>
    /// <param name="telemetry">Optional application telemetry for the policy.</param>
    /// <returns>A new resilience policy over a new writer connection pool.</returns>
    public static PostgresResilienceDbPolicy DbPolicyFixture<TSettings>(TSettings appSettings, IAppTelemetry? telemetry = null)
        where TSettings : IPostgresAppSettings, IResilienceAppSettings
    {
        var connectionPool = new PostgresConnectionPool(
            DataStorageInstanceType.Writer,
            new PostgresConnectionString(PostgresOptionsMapper.ConnectionOptions(appSettings)));

        var retryPolicyBuilder = new PostgresAsyncRetryPolicyBuilder(
            PostgresOptionsMapper.ResilienceOptions(appSettings),
            telemetry);

        return new PostgresResilienceDbPolicy(
            retryPolicyBuilder,
            connectionPool,
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<NpgsqlConnection>>(),
            telemetry);
    }

    /// <summary>
    ///     Build a <see cref="PostgresDbUnitOfWork" /> fixture from the test settings of
    ///     <typeparamref name="TAppSettings" />.
    /// </summary>
    /// <typeparam name="TAppSettings">The consumer's test settings type.</typeparam>
    /// <param name="telemetry">Optional application telemetry for the unit of work.</param>
    /// <returns>A new unit of work over a new writer connection pool.</returns>
    public static PostgresDbUnitOfWork DbUnitOfWorkFixture<TAppSettings>(IAppTelemetry? telemetry = null)
        where TAppSettings : class, IPostgresAppSettings
    {
        return DbUnitOfWorkFixture(TestHelper.TestSettings<TAppSettings>(), telemetry);
    }

    /// <summary>
    ///     Build a <see cref="PostgresDbUnitOfWork" /> fixture from the given settings.
    /// </summary>
    /// <param name="appSettings">The settings to build the fixture from.</param>
    /// <param name="telemetry">Optional application telemetry for the unit of work.</param>
    /// <returns>A new unit of work over a new writer connection pool.</returns>
    /// <remarks>
    ///     The unit of work uses its own async flow context, so repositories built from a separate
    ///     <see cref="DbPolicyFixture{TSettings}(TSettings, IAppTelemetry)" /> do not join its sessions.
    /// </remarks>
    public static PostgresDbUnitOfWork DbUnitOfWorkFixture(IPostgresAppSettings appSettings, IAppTelemetry? telemetry = null)
    {
        return new PostgresDbUnitOfWork(
            new PostgresConnectionPool(
                DataStorageInstanceType.Writer,
                new PostgresConnectionString(PostgresOptionsMapper.ConnectionOptions(appSettings))),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<NpgsqlConnection>>(),
            telemetry);
    }
}
