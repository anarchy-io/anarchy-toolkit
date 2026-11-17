using Anarchy.Toolkit.Web.Extension.Service.Builder;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.FileProviders;
using System.Runtime.CompilerServices;

namespace Anarchy.Toolkit.Web.Ui.Extension.Service.Builder;

/// <summary>
/// Extension methods for configuring ASP.NET Core application builder for UI applications.
/// </summary>
public static class ApplicationBuilderExtension
{
    private const string UiStaticAssetsRoot = "Anarchy.Toolkit.Web.Ui.wwwroot";

    private static readonly ConditionalWeakTable<IWebHostEnvironment, object> StaticWebAssetsLoaded = [];

    /// <param name="app">The application builder.</param>
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Serves the UI assets embedded in this package (<c>css/toolkit-ui.css</c>, <c>js/toolkit-ui.js</c>).
        /// </summary>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseToolkitUiStaticFiles()
        {
            return app.UseStaticFiles(new StaticFileOptions {
                FileProvider = new EmbeddedFileProvider(
                    typeof(ApplicationBuilderExtension).Assembly,
                    UiStaticAssetsRoot
                )
            });
        }

        /// <summary>
        /// Makes static web assets (e.g. Blazor's <c>_framework/blazor.web.js</c> and assets of referenced packages)
        /// available to <c>MapStaticAssets</c> when an unpublished build runs outside <c>Development</c>, e.g. in a
        /// <c>Local</c> or <c>Testing</c> environment or a test host, where ASP.NET Core leaves them out and interactive
        /// pages stay blank.
        /// </summary>
        /// <remarks>
        /// Does nothing for published applications (they have no runtime manifest; the assets are in <c>wwwroot</c>) and
        /// when already applied. In <c>Development</c>, where ASP.NET Core usually loads them already, it also covers
        /// hosts whose manifest is only known from the <c>staticWebAssets</c> setting. Call it before
        /// <c>MapStaticAssets</c>; <c>UseToolkitHostUi</c> calls it for you.
        /// </remarks>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseToolkitUiStaticWebAssets()
        {
            var environment = app.ApplicationServices.GetService<IWebHostEnvironment>();
            if (environment is null) return app;

            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
            var manifest = configuration[WebHostDefaults.StaticWebAssetsKey]
                ?? Path.Combine(AppContext.BaseDirectory, $"{environment.ApplicationName}.staticwebassets.runtime.json");
            if (!File.Exists(manifest)) return app;

            lock (StaticWebAssetsLoaded)
            {
                if (StaticWebAssetsLoaded.TryGetValue(environment, out _)) return app;
                StaticWebAssetsLoader.UseStaticWebAssets(environment, configuration);
                StaticWebAssetsLoaded.Add(environment, new object());
            }
            return app;
        }

        /// <summary>
        /// Configures the toolkit UI pipeline for a Blazor Web application.
        /// Includes static files, routing, authentication and antiforgery, but NOT endpoints.
        /// Call UseEndpoints afterwards to map the UI's endpoints.
        /// </summary>
        /// <remarks>
        /// Order: <c>UseToolkitUiStaticWebAssets</c>, the shared toolkit static files, <c>UseToolkitUiStaticFiles</c>, the common toolkit middlewares
        /// (trace/correlation id, HTTPS redirection, compression, routing, authentication, authorization) and
        /// antiforgery.
        /// </remarks>
        /// <param name="environment">The hosting environment.</param>
        /// <returns>The application builder, for chaining.</returns>
        public IApplicationBuilder UseToolkitHostUi(IHostEnvironment environment)
        {
            return app
                    .UseToolkitUiStaticWebAssets()
                    .UseToolkitStaticFiles()
                    .UseToolkitUiStaticFiles()
                    .UseToolkitCommonMiddlewares(environment)
                    .UseAntiforgery()
                ;
        }
    }
}
