using Anarchy.Toolkit.Core.Health.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Service.Health;
using Anarchy.Toolkit.Hosting.Workers.Service.Health.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Anarchy.Toolkit.Hosting.Workers.Extension.Microsoft.AspNetCore;

/// <summary>
/// Extensions to make it easier to apply <see cref="ApplicationHealthMiddleware"/> to your ASP.NET Core pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Applies the <see cref="ApplicationHealthMiddleware"/> to the request pipeline with a custom build metadata provider.
        /// </summary>
        public IApplicationBuilder UseApplicationHealthEndpointsWithCustomMetadata(IReadOnlyCollection<IServiceHealthReport> reports,
                IBuildMetadata buildMetadata,
                DateTimeOffset applicationStartTime
        ) =>
            app.UseMiddleware<ApplicationHealthMiddlewareWithCustomMetadata>(
                reports,
                buildMetadata,
                applicationStartTime
            );

        /// <summary>
        /// Applies the <see cref="ApplicationHealthMiddleware"/> that expects a <see cref="ApplicationHealthApi"/> injected on Invoke method.
        /// </summary>
        public IApplicationBuilder UseApplicationHealthEndpoints() =>
            app.UseMiddleware<ApplicationHealthMiddleware>();

        /// <summary>
        /// Applies the <see cref="ApplicationHealthMiddlewareWithScopedServices"/>, resolving the
        /// <see cref="ApplicationHealthApi"/> from a new scope of the given container per request.
        /// </summary>
        public IApplicationBuilder UseApplicationHealthEndpoints(IServiceScopeFactory scopeFactory) =>
            app.UseMiddleware<ApplicationHealthMiddlewareWithScopedServices>(scopeFactory);

        /// <summary>
        /// Applies the <see cref="ApplicationHealthMiddleware"/> to the request reading build metadata embedded
        /// in the specified assembly.
        /// </summary>
        public IApplicationBuilder UseApplicationHealthEndpoints(IReadOnlyCollection<IServiceHealthReport> reports,
                                                                 Assembly assembly,
                                                                 DateTimeOffset applicationStartTime
        ) =>
            app.UseMiddleware<ApplicationHealthMiddlewareWithMetadataFromAssembly>(
                reports,
                assembly,
                applicationStartTime
            );
    }
}
