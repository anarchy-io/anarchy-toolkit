using Anarchy.Toolkit.Web.MockServer.Ui.Service;
using Anarchy.Toolkit.Web.MockServer.Ui.Ui.Template;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Extension.DependencyInjection;
using Anarchy.Toolkit.Web.Ui.Extension.Service.Builder;

namespace Anarchy.Toolkit.Web.MockServer.Ui.Extension;

/// <summary>
/// <see cref="MockServerBuilder"/> extensions serving the mock server UI.
/// </summary>
public static class MockServerBuilderExtension
{
    /// <summary>The default path the UI is served under.</summary>
    public const string DefaultPath = MockServerHost.ControlPath + "/ui";

    // Keeps the UI's endpoints out of the application's global route table, so they never match mock requests.
    private const string GlobalEndpointRouteBuilderKey = "__GlobalEndpointRouteBuilder";

    extension(MockServerBuilder server)
    {
        /// <summary>
        /// Serves the UI (expectations, received requests, middlewares and live logs) under <paramref name="path"/>,
        /// unless <c>--no-ui</c> is given. Paths under <c>/__mock</c> are neither matched nor recorded.
        /// </summary>
        /// <example>
        /// <code language="csharp">
        /// return await MockServerEntryPoint.RunAsync(args, server =&gt; server
        ///     .AddExpectationsFrom("mocks")
        ///     .UseUi());
        /// </code>
        /// </example>
        /// <param name="path">The path, <see cref="DefaultPath"/> by default.</param>
        /// <returns>The builder.</returns>
        public MockServerBuilder UseUi(string path = DefaultPath)
        {
            path = "/" + path.Trim('/');
            var context = new MockServerUiContext();

            return server
                .ConfigureWebApplication(web => {
                    if (!server.Options.Ui) return;
                    // UseToolkitUi loads the static web assets (blazor.web.js); hosts without a manifest use this package's.
                    if (StaticWebAssets(web.Environment.ApplicationName, "runtime") is { } runtime)
                        web.WebHost.UseSetting(WebHostDefaults.StaticWebAssetsKey, runtime);
                    web.Services.AddSingleton(context);
                    web.Services.AddToolkitUi(ui => ui.UseRouting(new UiRoutingOptions { PathBase = path }));
                })
                .ExtendPipeline((host, app) => {
                    if (!server.Options.Ui) return;
                    context.Host = host;
                    app.Use((http, next) => {
                        if (http.Request.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                        {
                            http.Response.Redirect(path + "/");
                            return Task.CompletedTask;
                        }
                        return next(http);
                    });
                    app.Map(path, ui => {
                        ui.Properties.Remove(GlobalEndpointRouteBuilderKey);
                        ui.UseToolkitUi();
                        ui.UseRouting();
                        ui.UseAntiforgery();
                        ui.UseEndpoints(endpoints => {
                            if (StaticWebAssets(app.Environment.ApplicationName, "endpoints") is { } manifest) endpoints.MapStaticAssets(manifest);
                            endpoints.MapRazorComponents<App>().AddInteractiveServerRenderMode();
                        });
                    });
                })
                .AnnounceUi(host => server.Options.Ui ? host.BaseAddress.ToString().TrimEnd('/') + path + "/" : null);
        }
    }

    // The host's static web assets manifest, or this package's when the host has none (e.g. a test project).
    private static string? StaticWebAssets(string applicationName, string kind) =>
        new[] { applicationName, typeof(MockServerBuilderExtension).Assembly.GetName().Name! }
            .Select(name => Path.Combine(AppContext.BaseDirectory, $"{name}.staticwebassets.{kind}.json"))
            .FirstOrDefault(File.Exists);
}
