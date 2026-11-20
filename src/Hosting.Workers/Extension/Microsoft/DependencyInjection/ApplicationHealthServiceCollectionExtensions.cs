using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Service.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.DependencyInjection;

/// <summary>
/// Registers the services behind the application health endpoints and the worker health-check host.
/// </summary>
/// <remarks>
/// Besides <see cref="IBuildMetadata"/>, the required services are added only if not yet registered: an empty
/// <c>IReadOnlyCollection&lt;IHealthCheck&gt;</c>, a singleton <see cref="ApplicationInformation"/> and a
/// transient <see cref="ApplicationHealthApi"/>.
/// </remarks>
/// <example>
/// <code language="csharp">
/// services.AddServiceHealthCheckWorker(typeof(Program).Assembly);
/// </code>
/// </example>
public static class ApplicationHealthServiceCollectionExtensions
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Inject a singleton instance of <see cref="IBuildMetadata"/> and the required services to Health check service
        /// </summary>
        /// <typeparam name="TImplementation">The <see cref="IBuildMetadata"/> implementation.</typeparam>
        public void AddApplicationHealthEndpoints<TImplementation>()
            where TImplementation : class, IBuildMetadata
        {
            services.AddSingleton<IBuildMetadata, TImplementation>();
            AddRequiredServices(services);
        }

        /// <summary>
        /// Inject a singleton instance of <see cref="IBuildMetadata"/> and the required services to Health check service
        /// </summary>
        /// <remarks>Uses <see cref="BuildMetadataFromEmbeddedResource"/> for <paramref name="assembly"/>.</remarks>
        /// <param name="assembly">The assembly containing the embedded build metadata (normally the application's own).</param>
        public void AddApplicationHealthEndpoints(Assembly assembly)
        {
            services.AddSingleton<IBuildMetadata>(_ => new BuildMetadataFromEmbeddedResource(assembly));
            AddRequiredServices(services);
        }

        /// <summary>
        /// Registers the <see cref="ServiceHealthCheckWorker"/> and the health endpoints it serves, reading the build
        /// metadata embedded in <paramref name="assembly"/> (normally the worker application's own assembly).
        /// </summary>
        /// <remarks>
        /// The worker reads its port from <see cref="Anarchy.Toolkit.Configuration.Infrastructure.Entity.Configuration.Api.IWorkerHealthCheckAppSettings"/>,
        /// which must be registered.
        /// </remarks>
        /// <param name="assembly">The assembly containing the embedded build metadata.</param>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddServiceHealthCheckWorker(Assembly assembly)
        {
            services.AddApplicationHealthEndpoints(assembly);
            services.AddHostedService<ServiceHealthCheckWorker>();

            return services;
        }
    }

    private static void AddRequiredServices(IServiceCollection services)
    {
        // Health reports are registered as one IReadOnlyCollection (see AddDbHealthReports); an empty one keeps
        // the endpoints working when the application has none.
        services.TryAddSingleton<IReadOnlyCollection<IHealthCheck>>(Array.Empty<IHealthCheck>());
        services.TryAddSingleton<ApplicationInformation>();
        services.TryAddTransient<ApplicationHealthApi>();
    }
}
