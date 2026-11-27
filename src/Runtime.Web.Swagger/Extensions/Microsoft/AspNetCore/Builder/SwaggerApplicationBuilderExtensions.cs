using Anarchy.Toolkit.Bootstrap.Extensions.Microsoft.Extensions.Hosting;
using Anarchy.Toolkit.Runtime.Web.Swagger.Entity;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Anarchy.Toolkit.Runtime.Web.Swagger.Extensions.Microsoft.AspNetCore.Builder;

/// <summary>
/// <see cref="IApplicationBuilder"/> extensions that serve the toolkit Swagger UI and OpenAPI document.
/// </summary>
public static class SwaggerApplicationBuilderExtensions
{
    private const string SwaggerStaticAssetsRoot = "Anarchy.Toolkit.Runtime.Web.Swagger.wwwroot";

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Serves the OpenAPI document at <c>swagger/spec/swagger.json</c>, the Swagger UI at <c>/swagger</c> and its
        /// theme assets, and redirects <c>GET /</c> to the UI.
        /// </summary>
        /// <remarks>
        /// <c>AddToolkitSwagger</c> already adds it in front of the pipeline; call it yourself only after
        /// <c>AddToolkitSwaggerGen</c>, in a pipeline you compose. The theme stylesheets and script embedded in this
        /// package are served after the application's own <c>wwwroot</c>, so an application file at the same path
        /// (e.g. <c>wwwroot/css/swagger-colors.css</c>) replaces the packaged one. Swagger UI is themed according to
        /// <see cref="SwaggerThemeOptions"/> (enabled by default); outside the local environment the theme assets are
        /// prefixed with <c>/{EnvironmentName}</c>.
        /// </remarks>
        /// <param name="environment">The hosting environment.</param>
        /// <param name="apiName">The API name.</param>
        /// <param name="titlePrefix">Bracketed prefix for the Swagger UI title (e.g. "[API]").</param>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseToolkitSwagger(IHostEnvironment environment, string apiName, string titlePrefix = "[API]")
        {
            var baseUri = environment.IsLocal() ? string.Empty : "/" + environment.EnvironmentName;
            var theme = app.ApplicationServices.GetRequiredService<IOptions<SwaggerThemeOptions>>().Value;

            return app
                .UseToolkitSwaggerStaticFiles(environment)
                .UseToolkitSwaggerRootRedirect()
                .UseSwagger()
                .UseSwaggerUI(c => {
                    c.SwaggerEndpoint("spec/swagger.json", "Default");
                    c.DocumentTitle = $"{titlePrefix} {apiName}";
                    c.RoutePrefix = "swagger";
                    if (theme.Enabled) c.ApplySwaggerTheme(theme, baseUri);
                });
        }

        private IApplicationBuilder UseToolkitSwaggerStaticFiles(IHostEnvironment environment)
        {
            var embedded = new EmbeddedFileProvider(typeof(SwaggerApplicationBuilderExtensions).Assembly, SwaggerStaticAssetsRoot);
            var webRoot = (environment as IWebHostEnvironment)?.WebRootPath is { } root && Directory.Exists(root)
                ? new PhysicalFileProvider(root)
                : null;

            return app.UseStaticFiles(new StaticFileOptions {
                FileProvider = webRoot is null ? embedded : new CompositeFileProvider(webRoot, embedded)
            });
        }

        private IApplicationBuilder UseToolkitSwaggerRootRedirect()
        {
            return app.Use((context, next) => {
                if (!HttpMethods.IsGet(context.Request.Method) || context.Request.Path != "/") return next(context);

                context.Response.Redirect("swagger");
                return Task.CompletedTask;
            });
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
