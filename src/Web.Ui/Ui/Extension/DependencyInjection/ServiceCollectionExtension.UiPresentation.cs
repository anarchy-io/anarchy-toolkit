using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Presenter;
using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Service;

namespace Anarchy.Toolkit.Web.Ui.Ui.Extension.DependencyInjection;

public static partial class ServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the circuit-scoped UI services (notifications, JSON modal) and the generic presenters used by
        /// the toolkit components.
        /// </summary>
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
