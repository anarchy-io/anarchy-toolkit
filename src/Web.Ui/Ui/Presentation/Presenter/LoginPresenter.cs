using Anarchy.Toolkit.Configuration.Extension.Microsoft.Hosting;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;

namespace Anarchy.Toolkit.Web.Ui.Ui.Presentation.Presenter;

/// <summary>
/// Presenter for the login form. Handles environment selection and error mapping.
/// </summary>
public class LoginPresenter(
    IUiEnvironmentCatalog environmentCatalog,
    UiRoutingOptions routingOptions,
    IHostEnvironment hostEnvironment) : BasePresenter(routingOptions, hostEnvironment)
{
    // State properties
    public string ErrorMessage { get; set; } = string.Empty;
    public bool ShowEnvironmentSelector { get; set; }
    public string SelectedEnvironment { get; set; } = string.Empty;
    public IReadOnlyCollection<string> AvailableEnvironments { get; set; } = [];

    // Methods
    public void Initialize()
    {
        ShowEnvironmentSelector = HostEnvironment.IsLocal();
        SelectedEnvironment = HostEnvironment.EnvironmentName;
        AvailableEnvironments = environmentCatalog.Names;
    }

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
