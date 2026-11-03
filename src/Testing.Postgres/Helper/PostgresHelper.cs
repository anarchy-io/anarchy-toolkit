using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
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
public sealed class PostgresHelper
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
    ///     Build a <see cref="PostgresResilienceDbPolicy" /> fixture from the test settings of
    ///     <typeparamref name="TAppSettings" />.
    /// </summary>
    public static PostgresResilienceDbPolicy DbPolicyFixture<TAppSettings>()
        where TAppSettings : class, IPostgresAppSettings, IResilienceAppSettings
    {
        return DbPolicyFixture(TestHelper.TestSettings<TAppSettings>());
    }

    /// <summary>
    ///     Build a <see cref="PostgresResilienceDbPolicy" /> fixture from the given settings, which must expose both
    ///     the <c>Postgres</c> and <c>Resilience</c> sections.
    /// </summary>
    public static PostgresResilienceDbPolicy DbPolicyFixture<TSettings>(TSettings appSettings)
        where TSettings : IPostgresAppSettings, IResilienceAppSettings
    {
        var connectionPool = new PostgresConnectionPool(
            DataStorageInstanceType.Writer,
            new PostgresConnectionString(PostgresOptionsMapper.ConnectionOptions(appSettings)));

        var retryPolicyBuilder = new PostgresAsyncRetryPolicyBuilder(
            PostgresOptionsMapper.ResilienceOptions(appSettings),
            InMemoryObserver.Create());

        return new PostgresResilienceDbPolicy(
            retryPolicyBuilder,
            connectionPool,
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<NpgsqlConnection>>(),
            InMemoryObserver.Create());
    }

    /// <summary>
    ///     Build a <see cref="PostgresDbUnitOfWork" /> fixture from the test settings of
    ///     <typeparamref name="TAppSettings" />.
    /// </summary>
    public static PostgresDbUnitOfWork DbUnitOfWorkFixture<TAppSettings>()
        where TAppSettings : class, IPostgresAppSettings
    {
        return DbUnitOfWorkFixture(TestHelper.TestSettings<TAppSettings>());
    }

    /// <summary>
    ///     Build a <see cref="PostgresDbUnitOfWork" /> fixture from the given settings.
    /// </summary>
    public static PostgresDbUnitOfWork DbUnitOfWorkFixture(IPostgresAppSettings appSettings)
    {
        return new PostgresDbUnitOfWork(
            new PostgresConnectionPool(
                DataStorageInstanceType.Writer,
                new PostgresConnectionString(PostgresOptionsMapper.ConnectionOptions(appSettings))),
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<NpgsqlConnection>>(),
            InMemoryObserver.Create());
    }
}
