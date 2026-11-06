using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Mysql.DependencyInjection.Module;

/// <summary>Registers MySQL writer/reader pools and policies, retries and unit of work. Requires an Observer.</summary>
public sealed class ModuleMysql(
    Func<IConfiguration, MysqlConnectionOptions> connectionOptions,
    Func<IConfiguration, MysqlResilienceOptions> resilienceOptions) : IDependencyInjectionModule
{
    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton(connectionOptions(configuration))
            .AddSingleton(resilienceOptions(configuration))
            .AddSingleton<MysqlConnectionString>()
            .AddSingleton<IAsyncRetryPolicyBuilder<MySqlConnection>, MysqlAsyncRetryPolicyBuilder>()
            .AddSingleton<IAsyncFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>>, AsyncLocalFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>>>()
            .AddScoped<IUnitOfWork, MysqlDbUnitOfWork>()
            .AddSingleton<IStoragePolicy<MySqlConnection>>(sp => sp.GetRequiredKeyedService<IStoragePolicy<MySqlConnection>>(DataStorageInstanceType.Writer.ToDescription()));
        RegisterInstance(services, DataStorageInstanceType.Writer);
        RegisterInstance(services, DataStorageInstanceType.Reader);
        return services;
    }

    private static void RegisterInstance(IServiceCollection services, DataStorageInstanceType instanceType)
    {
        var key = instanceType.ToDescription();
        services.AddKeyedSingleton<IDbConnectionPool<MySqlConnection>>(key,
                (sp, _) => new MysqlConnectionPool(instanceType, sp.GetRequiredService<MysqlConnectionString>()))
            .AddKeyedSingleton<IStoragePolicy<MySqlConnection>>(key,
                (sp, _) => new MysqlResilienceDbPolicy(
                    sp.GetRequiredService<IAsyncRetryPolicyBuilder<MySqlConnection>>(),
                    sp.GetRequiredKeyedService<IDbConnectionPool<MySqlConnection>>(key),
                    sp.GetRequiredService<IAsyncFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>>>(),
                    sp.GetRequiredService<Observer>()));
    }
}
