using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Core.Observability.Entity.Service;
using Anarchy.Toolkit.Core.Observability.Extension;
using Anarchy.Toolkit.Core.Observability.Service.Observability;
using Anarchy.Toolkit.Hosting.Workers.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Service.Health.SelfHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Worker;

/// <summary>
/// Serves the worker's health endpoints on <see cref="WorkerHealthCheckSettings.ServicePort"/>. Every request runs
/// the <see cref="Anarchy.Toolkit.Core.Health.Service.Health.IServiceHealthReport"/>s registered in the worker's
/// container, in a scope of their own. Register it with <c>AddServiceHealthCheckWorker(assembly)</c>, which also
/// registers the <see cref="ApplicationHealthApi"/> it needs.
/// </summary>
public class ServiceHealthCheckWorker : IHostedService, IDisposable
{
    private readonly IWorkerHealthCheckAppSettings _appSettings;
    private readonly Observer _observer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SelfHostedApplicationHealthApi _healthApi;

    public ServiceHealthCheckWorker(IWorkerHealthCheckAppSettings appSettings,
                                    Observer observer,
                                    IServiceScopeFactory scopeFactory)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

        _healthApi = SelfHostedApplicationHealthApi.WithServices(_appSettings.WorkerHealthCheck.ServicePort,
                                                                 _scopeFactory);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var caller = this.CalledFrom();

        // Fail at startup, not on the first health request, when the health API was never registered.
        using (var scope = _scopeFactory.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ApplicationHealthApi>();
        }

        _healthApi.Start();

        _observer.LogInformation(
            (CallSiteEntity)caller,
            "Started listening for health requests on {Uri}",
            new Uri($"http://localhost:{_appSettings.WorkerHealthCheck.ServicePort}"));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _healthApi.Stop();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _healthApi.Dispose();
        GC.SuppressFinalize(this);
    }
}
