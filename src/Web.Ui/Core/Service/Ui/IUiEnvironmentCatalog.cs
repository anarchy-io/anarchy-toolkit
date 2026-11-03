using Anarchy.Toolkit.Web.Ui.Core.Entity.Ui;

namespace Anarchy.Toolkit.Web.Ui.Core.Service.Ui;

/// <summary>
/// Lists the environments a UI session can target.
/// </summary>
public interface IUiEnvironmentCatalog
{
    /// <summary>Names of every configured environment, in registration order.</summary>
    IReadOnlyCollection<string> Names { get; }

    /// <summary>
    /// Returns the environment with the given name, or the host's own environment when the name is empty.
    /// </summary>
    /// <exception cref="KeyNotFoundException">When the environment is not configured.</exception>
    UiEnvironment GetByName(string? environmentName);
}
