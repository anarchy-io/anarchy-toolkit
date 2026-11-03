using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Oracle.DependencyInjection.Module;

/// <summary>
/// Registers the Oracle persistence stack: connection string, keyed <c>writer</c>/<c>reader</c> connection pools
/// and storage policies, the retry policy builder, the async flow context and the <see cref="IUnitOfWork"/>.
/// The unkeyed <see cref="IStoragePolicy{TStorage}"/> resolves to the writer policy.
/// </summary>
/// <remarks>
/// Requires an <see cref="Observer"/> registration. The options are resolved from the application's configuration
/// through the given factories, so the module makes no assumption about section names or settings types.
/// </remarks>
public sealed class ModuleOracle(
    Func<IConfiguration, OracleConnectionOptions> connectionOptions,
    Func<IConfiguration, OracleResilienceOptions> resilienceOptions) : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration,
                                               IHostEnvironment environment)
    {
        services
            .AddSingleton(connectionOptions(configuration))
            .AddSingleton(resilienceOptions(configuration))
            .AddSingleton<OracleConnectionString>()
            .AddSingleton<IAsyncRetryPolicyBuilder<OracleConnection>, OracleAsyncRetryPolicyBuilder>()
            .AddSingleton<IAsyncFlowContext<StorageConnectionEnvelopeBase<OracleConnection>>,
                AsyncLocalFlowContext<StorageConnectionEnvelopeBase<OracleConnection>>>()
            .AddScoped<IUnitOfWork, OracleDbUnitOfWork>()
            .AddSingleton<IStoragePolicy<OracleConnection>>(sp =>
                sp.GetRequiredKeyedService<IStoragePolicy<OracleConnection>>(
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
            .AddKeyedSingleton<IDbConnectionPool<OracleConnection>>(key,
                (sp, _) => new OracleConnectionPool(instanceType, sp.GetRequiredService<OracleConnectionString>()))
            .AddKeyedSingleton<IStoragePolicy<OracleConnection>>(key,
                (sp, _) => new OracleResilienceDbPolicy(
                    sp.GetRequiredService<IAsyncRetryPolicyBuilder<OracleConnection>>(),
                    sp.GetRequiredKeyedService<IDbConnectionPool<OracleConnection>>(key),
                    sp.GetRequiredService<IAsyncFlowContext<StorageConnectionEnvelopeBase<OracleConnection>>>(),
                    sp.GetRequiredService<Observer>()))
            ;
    }
}
