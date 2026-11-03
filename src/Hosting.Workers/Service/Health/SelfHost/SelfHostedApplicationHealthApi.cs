using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.AspNetCore;
using Anarchy.Toolkit.Hosting.Workers.Service.Health.SelfHost.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.SelfHost
{
    /// <summary>
    /// A self-hosted application health API. This will start a self-hosted web server (Kestrel) on a port of your
    /// choosing and on that port will respond to the known health endpoints (/health/instance, /health/services,
    /// and /health/status). Disposing the instance stops the web server and frees the port.
    /// </summary>
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
        public static SelfHostedApplicationHealthApi WithMetadataFromAssembly(int portNumber,
                                                                              IReadOnlyCollection<IServiceHealthReport> reports,
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
        public static SelfHostedApplicationHealthApi WithCustomMetadata(int portNumber,
                                                                        IReadOnlyCollection<IServiceHealthReport> reports,
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
        public void Start()
        {
            ThrowIfAlreadyStarted();

            _host = _builder.CreateWebHost();
            _host.Start();
        }

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

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }

        private readonly HealthApiWebHostBuilder _builder;
        private IHost? _host;
    }
}
