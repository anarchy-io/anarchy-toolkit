namespace Anarchy.Toolkit.Bootstrap.Core.Service;

/// <summary>
/// Exposes the identifiers of the request or message currently being processed, for use in logs and outgoing calls.
/// </summary>
/// <remarks>
/// Registered as a singleton by <see cref="Anarchy.Toolkit.Bootstrap.DependencyInjection.Module.Configuration.Service.ModuleTelemetry"/>
/// with the <see cref="Anarchy.Toolkit.Bootstrap.Telemetry.ApplicationContext"/> implementation, whose
/// values come from async-local storage and therefore reflect the current async flow.
/// </remarks>
public interface IApplicationContext
{
    /// <summary>
    /// Gets the correlation id of the current request, or a process-wide fallback id when none is set.
    /// </summary>
    string RequestCorrelationId { get; }

    /// <summary>
    /// Gets the attempt id of the current request, or a process-wide fallback id when none is set.
    /// </summary>
    string RequestAttemptId { get; }
}
