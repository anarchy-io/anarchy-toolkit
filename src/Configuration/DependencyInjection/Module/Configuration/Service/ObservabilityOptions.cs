namespace Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service;

/// <summary>
/// Generic observability settings, bound from the <c>Observability</c> configuration section.
/// Organisation-specific log properties (team, cost center, ...) belong in a custom <see cref="ObserverFactory"/>.
/// </summary>
public record ObservabilityOptions
{
    /// <summary>
    /// Prefix prepended to the application name when it is missing (e.g. <c>MyCompany.</c>). Empty by default.
    /// </summary>
    public string AppNamePrefix { get; init; } = string.Empty;
}
