using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Microsoft.AspNetCore.Components.Web;

namespace Anarchy.Toolkit.Web.Ui.Presentation.Presenter;

/// <summary>
/// Builds the links rendered by the UI (honouring <see cref="UiRoutingOptions"/>) and hosts small UI helpers.
/// </summary>
/// <remarks>
/// Registered as a scoped service by <c>AddToolkitUiPresentation</c> and used as the base of the other toolkit
/// presenters. Links are prefixed with <c>/{environment-name}</c> (lower-cased) when
/// <see cref="UiRoutingOptions.EnableEnvironmentNamePathPrefix"/> is set and the host is not local.
/// </remarks>
/// <example>
/// <code language="csharp">
/// // With default options on a local host:
/// presenter.GetUiRoute("orders");          // "/ui/orders"
/// presenter.GetEndpointRoute("auth/logout"); // "/endpoint/auth/logout"
/// </code>
/// </example>
/// <param name="routingOptions">How rendered links are built.</param>
/// <param name="hostEnvironment">The host environment.</param>
public class BasePresenter(
    UiRoutingOptions routingOptions,
    IHostEnvironment hostEnvironment)
{
    /// <summary>The host environment.</summary>
    public readonly IHostEnvironment HostEnvironment =
        hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));

    /// <summary>
    /// The prefix applied to every link: <see cref="UiRoutingOptions.PathBase"/>, then <c>/{environment-name}</c> when
    /// the environment path prefix is enabled on a non-local host; an empty string when neither applies.
    /// </summary>
    public readonly string BaseHypertextReference = routingOptions.PathBase
                                                    + (!routingOptions.EnableEnvironmentNamePathPrefix
                                                       || hostEnvironment.IsLocal() ?
                                                        string.Empty :
                                                        "/" + hostEnvironment.EnvironmentName.ToLower());

    /// <summary>Builds an absolute link to <paramref name="route"/>, prefixed with <see cref="BaseHypertextReference"/>.</summary>
    /// <param name="route">The route, with or without a leading slash.</param>
    /// <returns>The link.</returns>
    public string GetRoute(string route = "")
    {
        return BaseHypertextReference + (route.StartsWith('/') ? route : '/' + route);
    }

    /// <summary>Builds a link to a Blazor page under <see cref="UiRoutingOptions.UiRoutePrefix"/>.</summary>
    /// <param name="route">The page route relative to the UI prefix; empty for the UI root.</param>
    /// <returns>The link.</returns>
    public string GetUiRoute(string route = "")
    {
        return GetRoute(BuildPrefixedRoute(routingOptions.UiRoutePrefix, route));
    }

    /// <summary>Builds a link to an HTTP endpoint under <see cref="UiRoutingOptions.EndpointRoutePrefix"/>.</summary>
    /// <param name="route">The endpoint route relative to the endpoint prefix; empty for the prefix root.</param>
    /// <returns>The link.</returns>
    public string GetEndpointRoute(string route = "")
    {
        return GetRoute(BuildPrefixedRoute(routingOptions.EndpointRoutePrefix, route));
    }

    private static string BuildPrefixedRoute(string prefix, string route)
    {
        var normalizedRoute = route.TrimStart('/');
        return string.IsNullOrEmpty(normalizedRoute) ? prefix : $"{prefix}/{normalizedRoute}";
    }

    /// <summary>Invokes <paramref name="action"/> when the pressed key is <c>Enter</c>.</summary>
    /// <param name="args">The keyboard event.</param>
    /// <param name="action">The asynchronous action to run.</param>
    /// <returns>The action's task, or a completed task for other keys.</returns>
    public static Task OnEnterKeyAsync(KeyboardEventArgs args, Func<Task> action)
    {
        return args.Key == "Enter" ? action() : Task.CompletedTask;
    }

    /// <summary>Invokes <paramref name="action"/> when the pressed key is <c>Enter</c>.</summary>
    /// <param name="args">The keyboard event.</param>
    /// <param name="action">The action to run.</param>
    public static void OnEnterKey(KeyboardEventArgs args, Action action)
    {
        if (args.Key == "Enter")
        {
            action();
        }
    }
}
