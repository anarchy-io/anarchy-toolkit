using Anarchy.Toolkit.Runtime.Web.Service.Host.Middleware;
using Microsoft.Extensions.FileProviders;

namespace Anarchy.Toolkit.Runtime.Web.Extensions.Microsoft.AspNetCore.Builder;

/// <summary>
/// <see cref="IApplicationBuilder"/> extensions that compose the toolkit HTTP request pipelines (shared middlewares,
/// static assets and the REST API / UI hosts).
/// </summary>
public static partial class ApplicationBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Serves the application's own <c>wwwroot</c> files, then the assets embedded in this package, so an
        /// application file overrides the packaged one at the same path (e.g. <c>favicon.svg</c>).
        /// </summary>
        private IApplicationBuilder UseToolkitRestApiStaticFiles(IHostEnvironment environment)
        {
            var embedded = new EmbeddedFileProvider(typeof(ApplicationBuilderExtensions).Assembly, SharedWebStaticAssetsRoot);
            var webRoot = (environment as IWebHostEnvironment)?.WebRootPath is { } root && Directory.Exists(root)
                ? new PhysicalFileProvider(root)
                : null;

            return app.UseStaticFiles(new StaticFileOptions {
                FileProvider = webRoot is null ? embedded : new CompositeFileProvider(webRoot, embedded)
            });
        }

        private IApplicationBuilder UseToolkitMiddlewaresRest(IHostEnvironment environment)
        {
            return app
                    .UseToolkitCommonMiddlewares(environment)
                    .UseMiddleware<LoggingMiddleware>()
                    .UseMiddleware<ExceptionMiddleware>() // Exception Middleware -> must be between UseRouting and UseEndpoints
                ;
        }

        private IApplicationBuilder UseToolkitEndpointsRest()
        {
            return app.UseEndpoints(routeBuilder => routeBuilder.MapControllers());
        }

        /// <summary>
        /// Configures the toolkit REST API pipeline.
        /// Includes static files, middleware, and endpoints.
        /// </summary>
        /// <remarks>
        /// Order: static files (the application's <c>wwwroot</c>, then the assets embedded in this package),
        /// <c>UseToolkitCommonMiddlewares</c>, then
        /// <see cref="Anarchy.Toolkit.Runtime.Web.Service.Host.Middleware.LoggingMiddleware"/> and
        /// <see cref="Anarchy.Toolkit.Runtime.Web.Service.Host.Middleware.ExceptionMiddleware"/>, and finally all
        /// controllers are mapped. Swagger is not part of it: <c>AddToolkitSwagger</c> from
        /// <c>Anarchy.Toolkit.Runtime.Web.Swagger</c> puts the Swagger UI and the <c>GET /</c> redirect in front of it.
        /// Called by <see cref="Anarchy.Toolkit.Runtime.Web.Composition.Startup.RestApiWebAppStartupBase{TAppSettingsDiModule, TAppSettings}.Configure"/>.
        /// </remarks>
        /// <param name="environment">The hosting environment.</param>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseToolkitHostRestApi(IHostEnvironment environment)
        {
            return app
                    .UseToolkitRestApiStaticFiles(environment)
                    .UseToolkitMiddlewaresRest(environment)
                    .UseToolkitEndpointsRest()
                ;
        }
    }
}
