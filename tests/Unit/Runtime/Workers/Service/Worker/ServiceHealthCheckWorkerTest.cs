using Anarchy.Toolkit.Bootstrap.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Runtime.Workers.Extensions.Microsoft.Extensions.DependencyInjection;
using Anarchy.Toolkit.Runtime.Workers.Service.Worker;
using Anarchy.Toolkit.Tests.Unit.Runtime.Workers.TestAssemblies;
using Anarchy.Toolkit.Tests.Unit.Runtime.Workers.TestHelpers;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Anarchy.Toolkit.Tests.Unit.Runtime.Workers.Service.Worker;

public class ServiceHealthCheckWorkerTest
{
    // Port 0: the operating system picks a free port when the health API starts.
    private ServiceHealthCheckWorker CreateSut(ServiceProvider provider) =>
        new(new WorkerHealthCheckAppSettings(0), AppTelemetry.Null,
            provider.GetRequiredService<IServiceScopeFactory>());

    [Fact]
    public async Task ServesTheScopedReportsRegisteredInTheWorkerContainer()
    {
        var services = new ServiceCollection();
        services.AddApplicationHealthEndpoints(WithMetadataHelper.Assembly);
        services.AddScoped<IReadOnlyCollection<IHealthCheck>>(_ => [
            new StubServiceHealthReport("worker report"),
            new StubServiceHealthReport("failing worker report", HealthCheckResult.Failed),
        ]);
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        using var sut = CreateSut(provider);
        await sut.StartAsync(TestContext.Current.CancellationToken);
        using var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{sut.HealthApiPort}") };
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
