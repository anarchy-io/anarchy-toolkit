using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;

namespace Anarchy.Toolkit.Web.Ui.Ui.Presentation.Presenter;

/// <summary>
/// Presenter for the login form. Handles environment selection and error mapping.
/// </summary>
/// <param name="environmentCatalog">The catalog providing the selectable environments.</param>
/// <param name="routingOptions">How rendered links are built.</param>
/// <param name="hostEnvironment">The host environment.</param>
public class LoginPresenter(
    IUiEnvironmentCatalog environmentCatalog,
    UiRoutingOptions routingOptions,
    IHostEnvironment hostEnvironment) : BasePresenter(routingOptions, hostEnvironment)
{
    // State properties
    /// <summary>The error message to display, or an empty string when there is none.</summary>
    public string ErrorMessage { get; set; } = string.Empty;
    /// <summary>Whether the environment selector is shown (local hosts only).</summary>
    public bool ShowEnvironmentSelector { get; set; }
    /// <summary>The environment selected in the form; initialized to the host's environment.</summary>
    public string SelectedEnvironment { get; set; } = string.Empty;
    /// <summary>The environment names offered by the selector.</summary>
    public IReadOnlyCollection<string> AvailableEnvironments { get; set; } = [];

    // Methods
    /// <summary>
    /// Initializes the form state from the host environment and the environment catalog.
    /// </summary>
    public void Initialize()
    {
        ShowEnvironmentSelector = HostEnvironment.IsLocal();
        SelectedEnvironment = HostEnvironment.EnvironmentName;
        AvailableEnvironments = environmentCatalog.Names;
    }

    /// <summary>
    /// Maps a <see cref="UiLoginErrors"/> code (from the <c>error</c> query parameter) to <see cref="ErrorMessage"/>.
    /// Unknown or missing codes clear the message.
    /// </summary>
    /// <param name="errorValue">The error code, or <see langword="null"/>.</param>
    public void SetErrorFromQueryParameter(string? errorValue)
    {
        ErrorMessage = errorValue switch {
            UiLoginErrors.InvalidCredentials => "Client ID and client secret are required.",
            UiLoginErrors.AuthFailed =>
                "Authentication failed. Check credentials and selected environment, then try again.",
            _ => string.Empty
        };
    }
}
