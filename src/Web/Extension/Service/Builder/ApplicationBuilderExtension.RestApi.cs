using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Web.Entity.Swagger;
using Anarchy.Toolkit.Web.Service.Host.Middleware;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Anarchy.Toolkit.Web.Extension.Service.Builder;

/// <summary>
/// <see cref="IApplicationBuilder"/> extensions that compose the toolkit HTTP request pipelines (shared middlewares,
/// static assets and the REST API / UI hosts).
/// </summary>
public static partial class ApplicationBuilderExtension
{
    extension(IApplicationBuilder app)
    {
        private IApplicationBuilder UseToolkitSwagger(string apiName,
                                                      IHostEnvironment environment,
                                                      string titlePrefix)
        {
            var baseUri = environment.IsLocal() ? string.Empty : "/" + environment.EnvironmentName;
            var theme = app.ApplicationServices.GetRequiredService<IOptions<SwaggerThemeOptions>>().Value;

            return app
                .UseSwagger()
                .UseSwaggerUI(c => {
                    c.SwaggerEndpoint("spec/swagger.json", "Default");
                    c.DocumentTitle = $"{titlePrefix} {apiName}";
                    c.RoutePrefix = "swagger";
                    if (theme.Enabled) c.ApplySwaggerTheme(theme, baseUri);
                });
        }

        /// <summary>
        /// Serves the application's own <c>wwwroot</c> files, then the assets embedded in this package, so an
        /// application file overrides the packaged one at the same path (e.g. <c>css/swagger-colors.css</c>).
        /// </summary>
        private IApplicationBuilder UseToolkitRestApiStaticFiles(IHostEnvironment environment)
        {
            var embedded = new EmbeddedFileProvider(typeof(ApplicationBuilderExtension).Assembly, SharedWebStaticAssetsRoot);
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
            return app.UseEndpoints(routeBuilder => {
                routeBuilder.MapGet("/", context => {
                    context.Response.Redirect("swagger");
                    return Task.CompletedTask;
                });

                routeBuilder.MapControllers();
            });
        }

        /// <summary>
        /// Configures the toolkit REST API pipeline.
        /// Includes static files, Swagger, middleware, and endpoints.
        /// </summary>
        /// <remarks>
        /// Order: static files (the application's <c>wwwroot</c>, then the assets embedded in this package), Swagger (JSON at <c>swagger/spec/swagger.json</c>, UI at
        /// <c>/swagger</c>), <c>UseToolkitCommonMiddlewares</c>, then
        /// <see cref="Anarchy.Toolkit.Web.Service.Host.Middleware.LoggingMiddleware"/> and
        /// <see cref="Anarchy.Toolkit.Web.Service.Host.Middleware.ExceptionMiddleware"/>, and finally endpoints:
        /// <c>GET /</c> redirects to <c>swagger</c> and all controllers are mapped. Outside the local environment the
        /// theme assets are prefixed with <c>/{EnvironmentName}</c>. Swagger UI is themed according to
        /// <see cref="SwaggerThemeOptions"/> (enabled by default); an application file at the same path as a
        /// packaged theme asset (e.g. <c>wwwroot/css/swagger-colors.css</c>) replaces it.
        /// Called by <see cref="Anarchy.Toolkit.Web.Composition.Startup.RestApiWebAppStartupBase{TAppSettingsDiModule, TAppSettings}.Configure"/>.
        /// </remarks>
        /// <param name="environment">The hosting environment.</param>
        /// <param name="apiName">The API name.</param>
        /// <param name="titlePrefix">Bracketed prefix for the Swagger title (e.g. "[API]").</param>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseToolkitHostRestApi(IHostEnvironment environment,
                                                         string apiName,
                                                         string titlePrefix = "[API]")
        {
            return app
                    .UseToolkitRestApiStaticFiles(environment)
                    .UseToolkitSwagger(apiName, environment, titlePrefix)
                    .UseToolkitMiddlewaresRest(environment)
                    .UseToolkitEndpointsRest()
                ;
        }
    }

    private static void ApplySwaggerTheme(this SwaggerUIOptions options, SwaggerThemeOptions theme, string baseUri)
    {
        options.InjectStylesheet(ResolveThemeAsset(theme.ColorsStylesheet, baseUri));
        if (theme.Stylesheet is { } stylesheet) options.InjectStylesheet(ResolveThemeAsset(stylesheet, baseUri));
        if (theme.Script is { } script) options.InjectJavascript(ResolveThemeAsset(script, baseUri));
        if (theme.Logo is { } logo)
        {
            var dark = theme.DarkLogo is { } darkLogo ? $"--swagger-theme-logo-dark: url('{ResolveThemeAsset(darkLogo, baseUri)}');" : string.Empty;
            options.HeadContent += $"<style>:root {{ --swagger-theme-logo: url('{ResolveThemeAsset(logo, baseUri)}'); {dark} " +
                                   "--swagger-theme-logo-display: block; --swagger-theme-logo-svg-display: none; }</style>";
        }
        if (theme.FallbackFavicon is { } fallback)
        {
            options.HeadContent += $"<link rel='icon' href='{ResolveThemeAsset(fallback, baseUri)}'>";
        }
        if (theme.Favicon is { } favicon)
        {
            var type = favicon.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ? " type='image/svg+xml' sizes='any'" : string.Empty;
            options.HeadContent += $"<link rel='icon'{type} href='{ResolveThemeAsset(favicon, baseUri)}'>";
        }
    }

    private static string ResolveThemeAsset(string path, string baseUri) =>
        path.StartsWith('/') || Uri.TryCreate(path, UriKind.Absolute, out _) ? path : $"{baseUri}/{path}";
}
