using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Core.Health.ValueSet.Service;
using Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.DependencyInjection;
using Anarchy.Toolkit.Hosting.Workers.Service.Worker;
using Anarchy.Toolkit.Testing;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestAssemblies;
using Anarchy.Toolkit.Tests.Unit.Hosting.Workers.TestHelpers;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Anarchy.Toolkit.Tests.Unit.Hosting.Workers.Service.Worker;

public class ServiceHealthCheckWorkerTest : TestCaseBase
{
    private readonly int _portNumber = EphemeralPortUtility.FindFreeEphemeralTcpPort();

    private ServiceHealthCheckWorker CreateSut(ServiceProvider provider) =>
        new(new WorkerHealthCheckAppSettings(_portNumber), FixtureObserver,
            provider.GetRequiredService<IServiceScopeFactory>());

    [Fact]
    public async Task ServesTheScopedReportsRegisteredInTheWorkerContainer()
    {
        var services = new ServiceCollection();
        services.AddApplicationHealthEndpoints(WithMetadataHelper.Assembly);
        services.AddScoped<IReadOnlyCollection<IServiceHealthReport>>(_ => [
            new StubServiceHealthReport("worker report"),
            new StubServiceHealthReport("failing worker report", HealthCheckResult.Failed),
        ]);
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        using var sut = CreateSut(provider);
        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{_portNumber}") };

        await sut.StartAsync(TestContext.Current.CancellationToken);
        using var response = await client.GetAsync("/health/services", TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        await sut.StopAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        content.Should().Contain("worker report").And.Contain("failing worker report");
    }

    [Fact]
    public async Task StartFailsWhenTheHealthApiIsNotRegistered()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        using var sut = CreateSut(provider);

        var act = () => sut.StartAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class WorkerHealthCheckAppSettings(int port) : IWorkerHealthCheckAppSettings
    {
        public WorkerHealthCheckSettings WorkerHealthCheck { get; init; } = new() { ServicePort = port };
    }
}
