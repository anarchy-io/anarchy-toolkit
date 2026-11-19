using Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api;
using Anarchy.Toolkit.Core.Telemetry;
using Anarchy.Toolkit.Hosting.Workers.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Service.Health.SelfHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
    private readonly IAppTelemetry _telemetry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SelfHostedApplicationHealthApi _healthApi;

    /// <summary>
    /// Creates the worker and prepares (without starting) the self-hosted health API.
    /// </summary>
    /// <param name="appSettings">Settings providing <see cref="WorkerHealthCheckSettings.ServicePort"/>.</param>
    /// <param name="telemetry">Application telemetry used for logs.</param>
    /// <param name="scopeFactory">Creates a scope per health request in the worker's container.</param>
    /// <exception cref="ArgumentNullException">Thrown if any argument is <see langword="null"/>.</exception>
    public ServiceHealthCheckWorker(IWorkerHealthCheckAppSettings appSettings,
                                     IAppTelemetry telemetry,
                                    IServiceScopeFactory scopeFactory)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

        _healthApi = SelfHostedApplicationHealthApi.WithServices(_appSettings.WorkerHealthCheck.ServicePort,
            _scopeFactory);
    }

    /// <summary>
    /// The port the health API listens on once started (see <see cref="SelfHostedApplicationHealthApi.Port"/>), which
    /// is the operating system's choice when <see cref="WorkerHealthCheckSettings.ServicePort"/> is <c>0</c>.
    /// </summary>
    public int? HealthApiPort => _healthApi.Port;

    /// <summary>
    /// Verifies that <see cref="ApplicationHealthApi"/> can be resolved and starts the health API.
    /// </summary>
    /// <param name="cancellationToken">Not used.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="ApplicationHealthApi"/> is not registered.</exception>
    /// <exception cref="Anarchy.Toolkit.Hosting.Workers.Service.Health.SelfHost.Exceptions.HealthApiAlreadyStartedException">
    /// Thrown if the health API was already started.
    /// </exception>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var caller = this.CalledFrom();

        // Fail at startup, not on the first health request, when the health API was never registered.
        using (var scope = _scopeFactory.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ApplicationHealthApi>();
        }

        _healthApi.Start();

        _telemetry.Log(
            LogLevel.Information,
            caller,
            null,
            "Started listening for health requests on {Uri}",
            new Uri($"http://localhost:{_appSettings.WorkerHealthCheck.ServicePort}"));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the health API.
    /// </summary>
    /// <param name="cancellationToken">Not used.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _healthApi.Stop();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the health API.
    /// </summary>
    public void Dispose()
    {
        _healthApi.Dispose();
        GC.SuppressFinalize(this);
    }
}
