using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Presenter;
using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Service;

namespace Anarchy.Toolkit.Web.Ui.Ui.Extension.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection"/> extensions registering the toolkit UI presentation layer.
/// </summary>
public static partial class ServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the circuit-scoped UI services (notifications, JSON modal) and the generic presenters used by
        /// the toolkit components.
        /// </summary>
        /// <remarks>
        /// Registers <see cref="UiRoutingOptions"/> as a singleton and <see cref="JsonModalState"/>,
        /// <see cref="NotificationService"/>, <see cref="BasePresenter"/>, <see cref="JsonRenderPresenter"/> and
        /// <see cref="LoginPresenter"/> as scoped services. <see cref="LoginPresenter"/> also requires the
        /// environment catalog registered by <c>AddToolkitUiAuthentication</c>. Called by
        /// <c>UiWebAppStartupBase</c>.
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
                    .AddScoped<LoginPresenter>()
                ;
        }
    }
}
