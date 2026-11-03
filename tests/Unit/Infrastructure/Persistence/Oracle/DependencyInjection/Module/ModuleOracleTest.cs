using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Oracle.Service.Storage.Database.Oracle;
using Anarchy.Toolkit.Testing.Helper;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Oracle.ManagedDataAccess.Client;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Oracle.DependencyInjection.Module;

public class ModuleOracleTest
{
    private static readonly OracleConnectionOptions ConnectionOptions = new(
        new Uri("tcp://reader.local"), new Uri("tcp://writer.local"), "user", "secret", "orders", "UTC", null, 30);

    private static ServiceProvider BuildProvider(Action<IConfiguration>? onResolveOptions = null)
    {
        var configuration = new ConfigurationBuilder().Build();

        return new ServiceCollection()
            .AddSingleton<Observer>(InMemoryObserver.Create())
            .UseModule(new ModuleOracle(
                    config => {
                        onResolveOptions?.Invoke(config);
                        return ConnectionOptions;
                    },
                    _ => new OracleResilienceOptions(3)),
                configuration,
                Mock.Of<IHostEnvironment>())
            .BuildServiceProvider();
    }

    [Fact]
    public void RegisterServices_PassesTheApplicationConfigurationToTheOptionsFactory()
    {
        IConfiguration? received = null;

        using var provider = BuildProvider(config => received = config);

        received.Should().NotBeNull();
        provider.GetRequiredService<OracleConnectionOptions>().Should().BeSameAs(ConnectionOptions);
        provider.GetRequiredService<OracleResilienceOptions>().RetryCount.Should().Be(3);
    }

    [Fact]
    public void RegisterServices_ResolvesTheUnitOfWork()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().BeOfType<OracleDbUnitOfWork>();
    }

    [Theory]
    [InlineData("writer")]
    [InlineData("reader")]
    public void RegisterServices_ResolvesKeyedConnectionPoolsAndPolicies(string key)
    {
        using var provider = BuildProvider();

        provider.GetRequiredKeyedService<IDbConnectionPool<OracleConnection>>(key)
            .Should().BeOfType<OracleConnectionPool>();
        provider.GetRequiredKeyedService<IStoragePolicy<OracleConnection>>(key)
            .Should().BeOfType<OracleResilienceDbPolicy>();
    }

    [Fact]
    public void RegisterServices_UnkeyedStoragePolicyIsTheWriterPolicy()
    {
        using var provider = BuildProvider();

        provider.GetRequiredService<IStoragePolicy<OracleConnection>>()
            .Should().BeSameAs(provider.GetRequiredKeyedService<IStoragePolicy<OracleConnection>>("writer"));
    }
}
