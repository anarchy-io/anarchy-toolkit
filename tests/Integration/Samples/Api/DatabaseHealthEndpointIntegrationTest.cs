using Anarchy.Toolkit.Core.Persistence.Abstractions.ValueSet.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Mysql.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Oracle.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Infrastructure.Persistence.Db.Postgres.Entity.Configuration.Storage.Database;
using Anarchy.Toolkit.Samples.Api.Composition;
using Anarchy.Toolkit.Samples.Api.Infrastructure.Entity.Configuration;
using Anarchy.Toolkit.Web.Presentation.Entity.Output.Health;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Anarchy.Toolkit.Tests.Integration.Samples.Api;

public class DatabaseHealthEndpointIntegrationTest
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task HealthServices_ReportsAllDatabaseProvidersAsPassed()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebApplicationFactory<SampleApiStartup>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/services", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {body}");
        var report = JsonSerializer.Deserialize<HealthCheckReportOutput>(body, JsonOptions);
        Assert.NotNull(report);
        Assert.True(report.Passed, body);

        var expected = new[] { "postgres", "mysql", "oracle" }
            .SelectMany(provider => new[] { "reader", "writer" }, (provider, instance) => $"{provider}_connection-{instance}")
            .Order()
            .ToArray();
        Assert.Equal(expected, report.Tests.Select(test => test.Name).Order().ToArray());
        Assert.All(report.Tests, test => Assert.Equal("passed", test.Result, ignoreCase: true));
    }

    [Fact]
    public async Task HealthServices_WhenDatabaseProviderIsUnavailable_ReturnsUnhealthyReport()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebApplicationFactory<SampleApiStartup>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Postgres:Port"] = "1" })));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/services", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var report = JsonSerializer.Deserialize<HealthCheckReportOutput>(body, JsonOptions);
        Assert.NotNull(report);
        Assert.False(report.Passed, body);
        Assert.All(report.Tests.Where(test => test.Name.StartsWith("postgres_", StringComparison.Ordinal)),
            test => Assert.Equal("failed", test.Result, ignoreCase: true));
    }

    [Fact]
    public async Task DatabaseHosts_BindAsUrisAndProduceLocalhostConnectionStrings()
    {
        await using var factory = new WebApplicationFactory<SampleApiStartup>();
        var services = factory.Services;
        var settings = services.GetRequiredService<AppSettings>();

        foreach (var db in new[] { settings.Postgres, settings.Mysql, settings.Oracle })
        {
            Assert.NotNull(db.HostReader);
            Assert.NotNull(db.HostWriter);
            Assert.Equal("localhost", db.HostReader.ToString());
            Assert.Equal("localhost", db.HostWriter.ToString());
        }

        var connectionStrings = new[]
        {
            services.GetRequiredService<PostgresConnectionString>().Value(DataStorageInstanceType.Writer),
            services.GetRequiredService<MysqlConnectionString>().Value(DataStorageInstanceType.Reader),
            services.GetRequiredService<OracleConnectionString>().Value(DataStorageInstanceType.Writer),
        };
        Assert.All(connectionStrings, value => Assert.Contains("localhost", value, StringComparison.OrdinalIgnoreCase));
    }
}
