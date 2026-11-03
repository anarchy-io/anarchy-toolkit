using Anarchy.Toolkit.Web.Ui.Core.Entity.Ui;
using Anarchy.Toolkit.Web.Ui.Core.Service.Ui;
using Anarchy.Toolkit.Web.Ui.Entity.Ui;

namespace Anarchy.Toolkit.Web.Ui.Model.Ui;

/// <summary>
/// Environment catalog backed by <see cref="UiAuthenticationOptions.Environments"/>.
/// </summary>
public sealed class UiEnvironmentCatalog(IHostEnvironment hostEnvironment, UiAuthenticationOptions options)
    : IUiEnvironmentCatalog
{
    private readonly IReadOnlyDictionary<string, UiEnvironment> _environmentsByName =
        options.Environments.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Names { get; } = options.Environments.Select(x => x.Name).ToArray();

    public UiEnvironment GetByName(string? environmentName)
    {
        var selectedEnvironment = string.IsNullOrEmpty(environmentName)
            ? hostEnvironment.EnvironmentName
            : environmentName;

        return !_environmentsByName.TryGetValue(selectedEnvironment, out var environment) ?
            throw new KeyNotFoundException($"Environment '{selectedEnvironment}' is not configured in the Catalog") :
            environment;
    }
}
