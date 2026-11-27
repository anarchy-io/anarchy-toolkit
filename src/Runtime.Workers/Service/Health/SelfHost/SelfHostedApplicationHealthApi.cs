using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Runtime.Workers.Extensions.Microsoft.AspNetCore.Builder;
using Anarchy.Toolkit.Runtime.Workers.Service.Health.SelfHost.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Anarchy.Toolkit.Runtime.Workers.Service.Health.SelfHost;

/// <summary>
/// A self-hosted application health API. This will start a self-hosted web server (Kestrel) on a port of your
/// choosing and on that port will respond to the known health endpoints (/health/instance, /health/services,
/// and /health/status). Disposing the instance stops the web server and frees the port.
/// </summary>
/// <remarks>
/// The server listens on <c>http://*:{port}</c>, enables systemd integration, and answers <c>404</c> to any other
/// path. Port <c>0</c> lets the operating system choose a free port when the server starts; <see cref="Port"/> then
/// tells which one, so tests never race for a port. Used by
/// <see cref="Anarchy.Toolkit.Runtime.Workers.Service.Worker.ServiceHealthCheckWorker"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var healthApi = SelfHostedApplicationHealthApi.WithServices(8081, serviceProvider.GetRequiredService&lt;IServiceScopeFactory&gt;());
/// healthApi.Start();
/// </code>
/// </example>
public class SelfHostedApplicationHealthApi : IDisposable
{
    private SelfHostedApplicationHealthApi(int portNumber,
                                           Action<IApplicationBuilder> configureAction)
    {
        _builder = new HealthApiWebHostBuilder(portNumber, configureAction);
    }

    /// <summary>
    /// Create a new instance of <see cref="SelfHostedApplicationHealthApi"/> reading build metadata embedded in
    /// the specified assembly, listening on the specified port.
    /// </summary>
    /// <param name="portNumber">The TCP port to listen on; <c>0</c> lets the operating system choose (see <see cref="Port"/>).</param>
    /// <param name="reports">The service health reports to run.</param>
    /// <param name="assembly">The assembly containing the embedded build metadata.</param>
    /// <param name="applicationStartTime">The time the application started, used to compute up-time.</param>
    /// <returns>A new, not yet started, health API.</returns>
    public static SelfHostedApplicationHealthApi WithMetadataFromAssembly(int portNumber,
                                                                          IReadOnlyCollection<IHealthCheck> reports,
                                                                          Assembly assembly,
                                                                          DateTimeOffset applicationStartTime)
    {
        void UseMiddleware(IApplicationBuilder app) =>
            app.UseApplicationHealthEndpoints(reports, assembly, applicationStartTime);

        return new SelfHostedApplicationHealthApi(portNumber, UseMiddleware);
    }

    /// <summary>
    /// Create a new instance of <see cref="SelfHostedApplicationHealthApi"/> with a custom build metadata
    /// provider, listening on the specified port.
    /// </summary>
    /// <param name="portNumber">The TCP port to listen on; <c>0</c> lets the operating system choose (see <see cref="Port"/>).</param>
    /// <param name="reports">The service health reports to run.</param>
    /// <param name="buildMetadata">The build metadata provider.</param>
    /// <param name="applicationStartTime">The time the application started, used to compute up-time.</param>
    /// <returns>A new, not yet started, health API.</returns>
    public static SelfHostedApplicationHealthApi WithCustomMetadata(int portNumber,
                                                                    IReadOnlyCollection<IHealthCheck> reports,
                                                                    IBuildMetadata buildMetadata,
                                                                    DateTimeOffset applicationStartTime)
    {
        void UseMiddleware(IApplicationBuilder app) =>
            app.UseApplicationHealthEndpointsWithCustomMetadata(reports, buildMetadata, applicationStartTime);

        return new SelfHostedApplicationHealthApi(portNumber, UseMiddleware);
    }

    /// <summary>
    /// Create a new instance of <see cref="SelfHostedApplicationHealthApi"/> listening on the specified port that
    /// resolves the <see cref="ApplicationHealthApi"/> (and with it the service health reports) from a new scope
    /// of the given container per request.
    /// </summary>
    /// <param name="portNumber">The TCP port to listen on; <c>0</c> lets the operating system choose (see <see cref="Port"/>).</param>
    /// <param name="scopeFactory">The scope factory of the container that registers <see cref="ApplicationHealthApi"/>.</param>
    /// <returns>A new, not yet started, health API.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="scopeFactory"/> is <see langword="null"/>.</exception>
    public static SelfHostedApplicationHealthApi WithServices(int portNumber, IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);

        void UseMiddleware(IApplicationBuilder app) =>
            app.UseApplicationHealthEndpoints(scopeFactory);

        return new SelfHostedApplicationHealthApi(portNumber, UseMiddleware);
    }

    /// <summary>
    /// Start the web server and start handling requests. This method can only be called once.
    /// </summary>
    /// <exception cref="HealthApiAlreadyStartedException">Thrown if the server was already started.</exception>
    public void Start()
    {
        ThrowIfAlreadyStarted();

        _host = _builder.CreateWebHost();
        _host.Start();
        Port = _host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses
            .Select(address => BindingAddress.Parse(address).Port)
            .FirstOrDefault();
    }

    /// <summary>
    /// The port the server listens on, known once <see cref="Start"/> returned: the requested port, or the one the
    /// operating system chose for port <c>0</c>. <see langword="null"/> before the server starts.
    /// </summary>
    public int? Port { get; private set; }

    private void ThrowIfAlreadyStarted()
    {
        if (_host is not null)
            throw new HealthApiAlreadyStartedException();
    }

    /// <summary>
    /// Stop handling requests and terminate the web server.
    /// </summary>
    public void Stop()
    {
        _host?.Dispose();
    }

    /// <summary>
    /// Stops the web server (see <see cref="Stop"/>).
    /// </summary>
    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private readonly HealthApiWebHostBuilder _builder;
    private IHost? _host;
}
