using Anarchy.Toolkit.Web.Ui.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Extension.DependencyInjection;
using Anarchy.Toolkit.Web.Ui.Ui.Presentation.Presenter;

namespace Anarchy.Toolkit.Web.Ui.Ui.Extension.DependencyInjection;

/// <summary>
/// Configures the opt-in add-ons of the toolkit UI registered by <c>AddToolkitUi</c>.
/// </summary>
/// <example>
/// <code language="csharp">
/// builder.Services.AddToolkitUi(ui =&gt; ui
///     .UseRouting(new UiRoutingOptions { EnableEnvironmentNamePathPrefix = true })
///     .AddCookieSession(new UiAuthenticationOptions { Scope = "my-api.read", Environments = [...] }));
/// </code>
/// </example>
/// <param name="services">The service collection the UI is registered in.</param>
public sealed class ToolkitUiBuilder(IServiceCollection services)
{
    /// <summary>The service collection the UI is registered in.</summary>
    public IServiceCollection Services { get; } = services;

    internal UiRoutingOptions RoutingOptions { get; private set; } = new();

    /// <summary>Sets how rendered links are built; defaults to a new <see cref="UiRoutingOptions"/>.</summary>
    /// <param name="options">The UI routing options.</param>
    /// <returns>The builder, for chaining.</returns>
    public ToolkitUiBuilder UseRouting(UiRoutingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        RoutingOptions = options;
        return this;
    }

    /// <summary>
    /// Adds the cookie session: OAuth client-credentials login, the environment catalog, cascading authentication
    /// state and <see cref="LoginPresenter"/>. Required by <c>LoginForm</c>, <c>SessionBadge</c>,
    /// <c>RootRedirect</c> and <c>UiApiClientBase</c>.
    /// </summary>
    /// <remarks>Calls <c>AddToolkitUiAuthentication</c>; map the login/logout endpoints with <c>MapToolkitUi</c>.</remarks>
    /// <param name="options">The UI authentication options.</param>
    /// <returns>The builder, for chaining.</returns>
    public ToolkitUiBuilder AddCookieSession(UiAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Services
            .AddToolkitUiAuthentication(options)
            .AddCascadingAuthenticationState()
            .AddScoped<LoginPresenter>();

        return this;
    }
}
