using Anarchy.Toolkit.Web.Extension.Service.Builder;
using Microsoft.Extensions.FileProviders;

namespace Anarchy.Toolkit.Web.Ui.Extension.Service.Builder;

/// <summary>
/// Extension methods for configuring ASP.NET Core application builder for UI applications.
/// </summary>
public static class ApplicationBuilderExtension
{
    private const string UiStaticAssetsRoot = "Anarchy.Toolkit.Web.Ui.wwwroot";

    /// <param name="app">The application builder.</param>
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Serves the UI assets embedded in this package (<c>toolkit-ui.css</c>, <c>toolkit-ui.js</c>).
        /// </summary>
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
        /// Configures the toolkit UI pipeline for a Blazor Web application.
        /// Includes static files, routing, authentication and antiforgery, but NOT endpoints.
        /// Call UseEndpoints afterwards to map the UI's endpoints.
        /// </summary>
        /// <param name="environment">The hosting environment.</param>
        public IApplicationBuilder UseToolkitHostUi(IHostEnvironment environment)
        {
            return app
                    .UseToolkitStaticFiles()
                    .UseToolkitUiStaticFiles()
                    .UseToolkitCommonMiddlewares(environment)
                    .UseAntiforgery()
                ;
        }
    }
}
