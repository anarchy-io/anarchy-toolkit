namespace Anarchy.Toolkit.Core.Telemetry;

/// <summary>Creates provider-neutral structured log scopes through application telemetry.</summary>
public sealed class LogContext(IAppTelemetry telemetry)
{
    /// <summary>Pushes properties until the returned scope is disposed.</summary>
    public IDisposable PushProperties(params KeyValuePair<string, object?>[] properties) => telemetry.BeginScope(properties);

    /// <summary>Pushes a single property until the returned scope is disposed.</summary>
    public IDisposable PushProperty(string name, object? value) => telemetry.BeginScope([new(name, value)]);
}
