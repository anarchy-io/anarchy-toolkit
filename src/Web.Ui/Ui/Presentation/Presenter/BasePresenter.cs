using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Microsoft.AspNetCore.Components.Web;

namespace Anarchy.Toolkit.Web.Ui.Ui.Presentation.Presenter;

/// <summary>
/// Builds the links rendered by the UI (honouring <see cref="UiRoutingOptions"/>) and hosts small UI helpers.
/// </summary>
public class BasePresenter(
    UiRoutingOptions routingOptions,
    IHostEnvironment hostEnvironment)
{
    public readonly IHostEnvironment HostEnvironment =
        hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));

    public readonly string BaseHypertextReference = !routingOptions.EnableEnvironmentNamePathPrefix
                                                    || hostEnvironment.IsLocal() ?
        string.Empty :
        "/" + hostEnvironment.EnvironmentName.ToLower();

    public string GetRoute(string route = "")
    {
        return BaseHypertextReference + (route.StartsWith('/') ? route : '/' + route);
    }

    public string GetUiRoute(string route = "")
    {
        return GetRoute(BuildPrefixedRoute(routingOptions.UiRoutePrefix, route));
    }

    public string GetEndpointRoute(string route = "")
    {
        return GetRoute(BuildPrefixedRoute(routingOptions.EndpointRoutePrefix, route));
    }

    private static string BuildPrefixedRoute(string prefix, string route)
    {
        var normalizedRoute = route.TrimStart('/');
        return string.IsNullOrEmpty(normalizedRoute) ? prefix : $"{prefix}/{normalizedRoute}";
    }

    public static Task OnEnterKeyAsync(KeyboardEventArgs args, Func<Task> action)
    {
        return args.Key == "Enter" ? action() : Task.CompletedTask;
    }

    public static void OnEnterKey(KeyboardEventArgs args, Action action)
    {
        if (args.Key == "Enter")
        {
            action();
        }
    }
}
