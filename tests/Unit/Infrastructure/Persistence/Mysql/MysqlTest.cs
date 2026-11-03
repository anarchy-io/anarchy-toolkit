using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Core.Composition.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Core.Extension;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Entity.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.Service.Storage;
using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Core.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Service.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.DependencyInjection.Module;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Health.Report.Mysql;
using Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Service.Storage.Database.Mysql;
using Anarchy.Toolkit.Testing.Helper;
using Anarchy.Toolkit.Testing.Mysql.Entity.Configuration;
using Anarchy.Toolkit.Testing.Mysql.Helper;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using MySqlConnector;
using Polly;
using Polly.Retry;
using System.Reflection;

namespace Anarchy.Toolkit.Tests.Unit.Infrastructure.Persistence.Mysql;

public class MysqlTest
{
    private static MysqlConnectionOptions Options(string password = "secret", uint? port = null) =>
        new(new Uri("tcp://reader.local"), new Uri("tcp://writer.local"), "user", password, "orders", "+00:00", port, 0);

    [Theory]
    [InlineData(DataStorageInstanceType.Reader, "reader.local")]
    [InlineData(DataStorageInstanceType.Writer, "writer.local")]
    public void ConnectionString_UsesSelectedHostAndEscapesCredentials(DataStorageInstanceType instance, string host)
    {
        var value = new MysqlConnectionString(Options("a;\"b", 3307)).Value(instance);
        var parsed = new MySqlConnectionStringBuilder(value);
        parsed.Server.Should().Be(host);
        parsed.Port.Should().Be(3307);
        parsed.Password.Should().Be("a;\"b");
        parsed.DefaultCommandTimeout.Should().Be(1);
        parsed.IgnoreCommandTransaction.Should().BeTrue();
    }

    [Fact]
    public void ConnectionString_DefaultsAndValidation()
    {
        var value = new MysqlConnectionString(Options()).Value(DataStorageInstanceType.Writer);
        new MySqlConnectionStringBuilder(value).Port.Should().Be(3306);
        var act = () => new MysqlConnectionString(Options("")).Value(DataStorageInstanceType.Writer);
        act.Should().Throw<Exception>().WithMessage("*Invalid Mysql connection settings*");
    }

    private static MySqlException Error(int number) => (MySqlException)Activator.CreateInstance(
        typeof(MySqlException), BindingFlags.Instance | BindingFlags.NonPublic, null,
        [(MySqlErrorCode)number, "error"], null)!;

    [Fact]
    public void Dialect_ClassifiesErrorsAndUsesMysqlSql()
    {
        MysqlDbDialect.Name.Should().Be("mysql");
        MysqlDbDialect.IsUniqueViolation(Error(1062)).Should().BeTrue();
        MysqlDbDialect.IsNoData(Error(1329)).Should().BeTrue();
        MysqlDbDialect.IsUniqueViolation(Error(1329)).Should().BeFalse();
        MysqlDbDialect.IsServerError(Error(1062)).Should().BeTrue();
        MysqlDbDialect.IsServerError(Error(1213)).Should().BeFalse();
        MysqlDbDialect.IsServerError(Error(2013)).Should().BeFalse();
        MysqlDbDialect.IsTransient(Error(1213)).Should().BeTrue();
        MysqlDbDialect.IsTransient(new TimeoutException()).Should().BeTrue();
        MysqlDbDialect.IsTransient(Error(1062)).Should().BeFalse();
        CanSelectFromInsert<MysqlDbDialect>().Should().BeFalse();
        HealthSql<MysqlDbDialect>().Should().Be("SELECT CURRENT_TIMESTAMP");
        TruncateSql<MysqlDbDialect>().Should().Be("TRUNCATE TABLE items");
        SavepointSql<MysqlDbDialect>().Should().Be("SAVEPOINT sp");
        var inputs = new object[] { new { Id = 1 }, new { Id = 2 } };
        BatchParameters<MysqlDbDialect>(inputs).Should().BeSameAs(inputs);
    }

    private static bool CanSelectFromInsert<T>() where T : IDbDialect => T.CanSelectFromInsert;
    private static string HealthSql<T>() where T : IDbDialect => T.HealthCheckSql;
    private static string TruncateSql<T>() where T : IDbDialect => T.TruncateTableSql("items");
    private static string? SavepointSql<T>() where T : IDbDialect => T.SavepointCreateSql("sp");
    private static object BatchParameters<T>(IReadOnlyList<object> inputs) where T : IDbDialect => T.ToBatchParameters(inputs);

    [Fact]
    public void Module_ResolvesPoolsPoliciesUnitOfWorkAndHealthReports()
    {
        using var provider = new ServiceCollection()
            .AddSingleton<Observer>(InMemoryObserver.Create())
            .UseModule(new ModuleMysql(_ => Options(), _ => new MysqlResilienceOptions(3)),
                new ConfigurationBuilder().Build(), Mock.Of<IHostEnvironment>())
            .AddMysqlHealthReports().BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IUnitOfWork>().Should().BeOfType<MysqlDbUnitOfWork>();
        foreach (var key in new[] { "reader", "writer" })
        {
            provider.GetRequiredKeyedService<IDbConnectionPool<MySqlConnection>>(key).Should().BeOfType<MysqlConnectionPool>();
            provider.GetRequiredKeyedService<IStoragePolicy<MySqlConnection>>(key).Should().BeOfType<MysqlResilienceDbPolicy>();
        }
        provider.GetRequiredService<IStoragePolicy<MySqlConnection>>()
            .Should().BeSameAs(provider.GetRequiredKeyedService<IStoragePolicy<MySqlConnection>>("writer"));
        provider.GetRequiredService<IReadOnlyCollection<IServiceHealthReport>>().Should().HaveCount(2);
    }

    [Fact]
    public async Task HealthReports_IdentifyReaderAndWriterAndReportFailures()
    {
        var policy = new Mock<IStoragePolicy<MySqlConnection>>();
        var observer = InMemoryObserver.Create();
        var reader = await new MysqlHealthReportReader(policy.Object, observer).RunAsync();
        reader.Name.Should().Be("mysql_connection-reader");
        reader.Result.Should().Be(HealthCheckResult.Passed.ToDescription());
        policy.Setup(x => x.ExecuteAsync(It.IsAny<StoragePolicyCallback<MySqlConnection, DateTime>>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken?>())).ThrowsAsync(new Exception("unavailable"));
        var writer = await new MysqlHealthReportWriter(policy.Object, observer).RunAsync();
        writer.Name.Should().Be("mysql_connection-writer");
        writer.Result.Should().Be(HealthCheckResult.Failed.ToDescription());
    }

    [Fact]
    public void Fixtures_MapSettingsAndRefreshCachedDependencies()
    {
        var settings = new Settings();
        MysqlOptionsMapper.ConnectionOptions(settings).HostWriter.Should().Be(settings.Mysql.HostWriter);
        MysqlOptionsMapper.ResilienceOptions(settings).RetryCount.Should().Be(2);
        MysqlHelper.DbPolicyFixture(settings).Should().NotBeNull();
        MysqlHelper.DbUnitOfWorkFixture(settings).Should().NotBeNull();
        var policy = SingletonMysqlHelper.DbPolicyFixture(settings, true);
        SingletonMysqlHelper.DbPolicyFixture(settings).Should().BeSameAs(policy);
        SingletonMysqlHelper.RetryPolicyBuilder(settings, true);
        SingletonMysqlHelper.DbPolicyFixture(settings).Should().NotBeSameAs(policy);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 3)]
    public async Task ResiliencePolicy_OnlyRetriesOutsideTransaction(bool inTransaction, int expectedAttempts)
    {
        var envelope = new Anarchy.Toolkit.Infrastructure.Persistence.Mysql.Entity.Storage.Database.MysqlConnectionEnvelope(
            new MySqlConnection(), DataStorageInstanceType.Writer) { IsInTransaction = inTransaction };
        var pool = new Mock<IDbConnectionPool<MySqlConnection>>();
        pool.Setup(x => x.ConnectAsync(It.IsAny<CancellationToken?>())).ReturnsAsync(envelope);
        var retry = new Mock<IAsyncRetryPolicyBuilder<MySqlConnection>>();
        retry.Setup(x => x.Build()).Returns(new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.Zero,
            ShouldHandle = new PredicateBuilder().Handle<TimeoutException>()
        }).Build());
        var policy = new MysqlResilienceDbPolicy(retry.Object, pool.Object,
            new AsyncLocalFlowContext<StorageConnectionEnvelopeBase<MySqlConnection>>());
        var attempts = 0;
        Func<Task> act = () => policy.ExecuteAsync<int>((_, _) => { attempts++; throw new TimeoutException(); });
        await act.Should().ThrowAsync<TimeoutException>();
        attempts.Should().Be(expectedAttempts);
    }

    private sealed class Settings : IMysqlAppSettings, IResilienceAppSettings
    {
        public DbConnectionSettings Mysql { get; init; } = new() {
            HostReader = new Uri("tcp://reader.local"),
            HostWriter = new Uri("tcp://writer.local"),
            Username = "user",
            Password = "secret",
            DbName = "orders",
            TimeZone = "+00:00"
        };
        public ResilienceSettings Resilience { get; init; } = new() { RetryCount = 2 };
    }
}
