using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Postgres.DependencyInjection.Module;

/// <summary>
/// Registers the PostgreSQL persistence stack: connection string, keyed <c>writer</c>/<c>reader</c> connection pools
/// and storage policies, the retry policy builder, the async flow context and the <see cref="IUnitOfWork"/>.
/// The unkeyed <see cref="IStoragePolicy{TStorage}"/> resolves to the writer policy.
/// </summary>
/// <remarks>
/// Requires an <see cref="Observer"/> registration. The options are resolved from the application's configuration
/// through the given factories, so the module makes no assumption about section names or settings types.
/// </remarks>
public sealed class ModulePostgres(
    Func<IConfiguration, PostgresConnectionOptions> connectionOptions,
    Func<IConfiguration, PostgresResilienceOptions> resilienceOptions) : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        services
            .AddSingleton(connectionOptions(configuration))
            .AddSingleton(resilienceOptions(configuration))
            .AddSingleton<PostgresConnectionString>()
            .AddSingleton<IAsyncRetryPolicyBuilder<NpgsqlConnection>, PostgresAsyncRetryPolicyBuilder>()
            .AddSingleton<IAsyncFlowContext<StorageConnectionEnvelopeBase<NpgsqlConnection>>,
                AsyncLocalFlowContext<StorageConnectionEnvelopeBase<NpgsqlConnection>>>()
            .AddScoped<IUnitOfWork, PostgresDbUnitOfWork>()
            .AddSingleton<IStoragePolicy<NpgsqlConnection>>(sp =>
                sp.GetRequiredKeyedService<IStoragePolicy<NpgsqlConnection>>(
                    DataStorageInstanceType.Writer.ToDescription()))
            ;

        RegisterInstance(services, DataStorageInstanceType.Writer);
        RegisterInstance(services, DataStorageInstanceType.Reader);

        return services;
    }

    private static void RegisterInstance(IServiceCollection services, DataStorageInstanceType instanceType)
    {
        var key = instanceType.ToDescription();

        services
            .AddKeyedSingleton<IDbConnectionPool<NpgsqlConnection>>(key,
                (sp, _) => new PostgresConnectionPool(instanceType, sp.GetRequiredService<PostgresConnectionString>()))
            .AddKeyedSingleton<IStoragePolicy<NpgsqlConnection>>(key,
                (sp, _) => new PostgresResilienceDbPolicy(
                    sp.GetRequiredService<IAsyncRetryPolicyBuilder<NpgsqlConnection>>(),
                    sp.GetRequiredKeyedService<IDbConnectionPool<NpgsqlConnection>>(key),
                    sp.GetRequiredService<IAsyncFlowContext<StorageConnectionEnvelopeBase<NpgsqlConnection>>>(),
                    sp.GetRequiredService<Observer>()))
            ;
    }
}
