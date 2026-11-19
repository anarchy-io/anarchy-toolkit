using Anarchy.Toolkit.Core.Composition.Module;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Service.Storage.Database.Mysql;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;

namespace Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.DependencyInjection.Module;

/// <summary>Registers MySQL writer/reader pools and policies, retries and unit of work. Requires application telemetry.</summary>
/// <param name="connectionOptions">Factory building the connection options from <see cref="IConfiguration"/>.</param>
/// <param name="resilienceOptions">Factory building the resilience (retry) options from <see cref="IConfiguration"/>.</param>
/// <example>
/// <code language="csharp">
/// var module = new ModuleMysql(
///     cfg =&gt; cfg.GetSection("Database").Get&lt;MysqlConnectionOptions&gt;()!,
///     cfg =&gt; new MysqlResilienceOptions(RetryCount: 3));
/// module.RegisterServices(builder.Services, builder.Configuration, builder.Environment);
///
/// // In a repository: unkeyed policy = writer; [FromKeyedServices("reader")] = reader.
/// </code>
/// </example>
public sealed class ModuleMysql(
    Func<IConfiguration, MysqlConnectionOptions> connectionOptions,
    Func<IConfiguration, MysqlResilienceOptions> resilienceOptions) : IDependencyInjectionModule
{
    /// <inheritdoc/>
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
                    sp.GetRequiredService<IAppTelemetry>()));
    }
}
