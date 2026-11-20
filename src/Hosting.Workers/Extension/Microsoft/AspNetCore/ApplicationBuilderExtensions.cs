using Anarchy.Toolkit.Core.Health.Checks;
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
/// <remarks>
/// All variants serve <c>/health/instance</c>, <c>/health/services</c> and <c>/health/status</c> (see
/// <see cref="ApplicationHealthMiddlewareBase"/>); they differ in how the <see cref="ApplicationHealthApi"/> is obtained.
/// </remarks>
/// <example>
/// <code language="csharp">
/// app.UseApplicationHealthEndpoints(); // requires services.AddApplicationHealthEndpoints(typeof(Program).Assembly)
/// </code>
/// </example>
public static class ApplicationBuilderExtensions
{
    /// <param name="app">The application builder.</param>
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Applies the <see cref="ApplicationHealthMiddlewareWithCustomMetadata"/> to the request pipeline with a custom
        /// build metadata provider.
        /// </summary>
        /// <param name="reports">The service health reports to run.</param>
        /// <param name="buildMetadata">The build metadata provider.</param>
        /// <param name="applicationStartTime">The time the application started, used to compute up-time.</param>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseApplicationHealthEndpointsWithCustomMetadata(IReadOnlyCollection<IHealthCheck> reports,
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
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseApplicationHealthEndpoints() =>
            app.UseMiddleware<ApplicationHealthMiddleware>();

        /// <summary>
        /// Applies the <see cref="ApplicationHealthMiddlewareWithScopedServices"/>, resolving the
        /// <see cref="ApplicationHealthApi"/> from a new scope of the given container per request.
        /// </summary>
        /// <param name="scopeFactory">The scope factory of the container that registers <see cref="ApplicationHealthApi"/>.</param>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseApplicationHealthEndpoints(IServiceScopeFactory scopeFactory) =>
            app.UseMiddleware<ApplicationHealthMiddlewareWithScopedServices>(scopeFactory);

        /// <summary>
        /// Applies the <see cref="ApplicationHealthMiddlewareWithMetadataFromAssembly"/> to the request pipeline, reading
        /// build metadata embedded in the specified assembly.
        /// </summary>
        /// <param name="reports">The service health reports to run.</param>
        /// <param name="assembly">The assembly containing the embedded build metadata.</param>
        /// <param name="applicationStartTime">The time the application started, used to compute up-time.</param>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseApplicationHealthEndpoints(IReadOnlyCollection<IHealthCheck> reports,
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
