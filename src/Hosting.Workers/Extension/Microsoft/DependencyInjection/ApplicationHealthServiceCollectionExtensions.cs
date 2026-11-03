using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Service.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.DependencyInjection
{
    public static class ApplicationHealthServiceCollectionExtensions
    {
        extension(IServiceCollection services)
        {
            /// <summary>
            /// Inject a singleton instance of <see cref="IBuildMetadata"/> and the required services to Health check service
            /// </summary>
            public void AddApplicationHealthEndpoints<TImplementation>()
                where TImplementation : class, IBuildMetadata
            {
                services.AddSingleton<IBuildMetadata, TImplementation>();
                AddRequiredServices(services);
            }

            /// <summary>
            /// Inject a singleton instance of <see cref="IBuildMetadata"/> and the required services to Health check service
            /// </summary>
            public void AddApplicationHealthEndpoints(Assembly assembly)
            {
                services.AddSingleton<IBuildMetadata>(_ => new BuildMetadataFromEmbeddedResource(assembly));
                AddRequiredServices(services);
            }

            /// <summary>
            /// Registers the <see cref="ServiceHealthCheckWorker"/> and the health endpoints it serves, reading the build
            /// metadata embedded in <paramref name="assembly"/> (normally the worker application's own assembly).
            /// </summary>
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
            services.TryAddSingleton<IReadOnlyCollection<IServiceHealthReport>>(Array.Empty<IServiceHealthReport>());
            services.TryAddSingleton<ApplicationInformation>();
            services.TryAddTransient<ApplicationHealthApi>();
        }
    }
}
