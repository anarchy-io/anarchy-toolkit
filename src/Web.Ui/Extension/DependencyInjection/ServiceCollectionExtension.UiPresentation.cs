using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Presentation.Presenter;
using Anarchy.Toolkit.Web.Ui.Presentation.Service;

namespace Anarchy.Toolkit.Web.Ui.Extension.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extensions registering the toolkit UI presentation layer.
/// </summary>
public static partial class ServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the toolkit UI on any web application: Razor components with interactive server rendering, the
        /// HTTP context accessor and the presentation services. No login is required; the cookie session is an
        /// opt-in add-on (<see cref="ToolkitUiBuilder.AddCookieSession"/>).
        /// </summary>
        /// <example>
        /// <code language="csharp">
        /// builder.Services.AddToolkitUi();
        /// // ...
        /// app.UseToolkitUi();
        /// app.MapToolkitUi();
        /// app.MapRazorComponents&lt;App&gt;().AddInteractiveServerRenderMode();
        /// </code>
        /// </example>
        /// <param name="configure">Configures the routing options and the opt-in add-ons.</param>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddToolkitUi(Action<ToolkitUiBuilder>? configure = null)
        {
            var builder = new ToolkitUiBuilder(services);
            configure?.Invoke(builder);

            services.AddHttpContextAccessor();
            services.AddRazorComponents().AddInteractiveServerComponents();

            return services.AddToolkitUiPresentation(builder.RoutingOptions);
        }

        /// <summary>
        /// Registers the circuit-scoped UI services (notifications, JSON modal) and the generic presenters used by
        /// the toolkit components.
        /// </summary>
        /// <remarks>
        /// Registers <see cref="UiRoutingOptions"/> as a singleton and <see cref="JsonModalState"/>,
        /// <see cref="NotificationService"/>, <see cref="BasePresenter"/> and <see cref="JsonRenderPresenter"/> as
        /// scoped services. <see cref="LoginPresenter"/> is registered by the cookie session add-on
        /// (<see cref="ToolkitUiBuilder.AddCookieSession"/>). Called by <c>AddToolkitUi</c>.
        /// </remarks>
        /// <param name="routingOptions">How rendered links are built; defaults to a new <see cref="UiRoutingOptions"/>.</param>
        /// <returns>The service collection, for chaining.</returns>
        public IServiceCollection AddToolkitUiPresentation(UiRoutingOptions? routingOptions = null)
        {
            return services
                    .AddSingleton(routingOptions ?? new UiRoutingOptions())
                    .AddScoped<JsonModalState>()
                    .AddScoped<NotificationService>()
                    .AddScoped<BasePresenter>()
                    .AddScoped<JsonRenderPresenter>()
                ;
        }
    }
}
