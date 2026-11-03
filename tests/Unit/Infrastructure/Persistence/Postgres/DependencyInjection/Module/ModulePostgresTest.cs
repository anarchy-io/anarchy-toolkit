using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Postgres.Service.Storage.Database.Postgres;
using Anarchy.Toolkit.Testing.Helper;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Npgsql;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Postgres.DependencyInjection.Module;

public class ModulePostgresTest
{
    private static readonly PostgresConnectionOptions ConnectionOptions = new(
        new Uri("tcp://reader.local"), new Uri("tcp://writer.local"), "user", "secret", "orders", "UTC", null, 30);

    private static ServiceProvider BuildProvider(Action<IConfiguration>? onResolveOptions = null)
    {
        var configuration = new ConfigurationBuilder().Build();

        return new ServiceCollection()
            .AddSingleton<Observer>(InMemoryObserver.Create())
            .UseModule(new ModulePostgres(
                    config => {
                        onResolveOptions?.Invoke(config);
                        return ConnectionOptions;
                    },
                    _ => new PostgresResilienceOptions(3)),
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
        provider.GetRequiredService<PostgresConnectionOptions>().Should().BeSameAs(ConnectionOptions);
        provider.GetRequiredService<PostgresResilienceOptions>().RetryCount.Should().Be(3);
    }

    [Fact]
    public void RegisterServices_ResolvesTheUnitOfWork()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().BeOfType<PostgresDbUnitOfWork>();
    }

    [Theory]
    [InlineData("writer")]
    [InlineData("reader")]
    public void RegisterServices_ResolvesKeyedConnectionPoolsAndPolicies(string key)
    {
        using var provider = BuildProvider();

        provider.GetRequiredKeyedService<IDbConnectionPool<NpgsqlConnection>>(key)
            .Should().BeOfType<PostgresConnectionPool>();
        provider.GetRequiredKeyedService<IStoragePolicy<NpgsqlConnection>>(key)
            .Should().BeOfType<PostgresResilienceDbPolicy>();
    }

    [Fact]
    public void RegisterServices_UnkeyedStoragePolicyIsTheWriterPolicy()
    {
        using var provider = BuildProvider();

        provider.GetRequiredService<IStoragePolicy<NpgsqlConnection>>()
            .Should().BeSameAs(provider.GetRequiredKeyedService<IStoragePolicy<NpgsqlConnection>>("writer"));
    }
}
