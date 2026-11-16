namespace Anarchy.Toolkit.Configuration.Core.Service;

/// <summary>
/// Exposes the identifiers of the request or message currently being processed, for use in logs and outgoing calls.
/// </summary>
/// <remarks>
/// Registered as a singleton by <see cref="Anarchy.Toolkit.Configuration.DependencyInjection.Module.Configuration.Service.ModuleObservability"/>
/// with the <see cref="Anarchy.Toolkit.Configuration.Entity.Observability.ApplicationContext"/> implementation, whose
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

    /// <summary>
    /// Gets the AWS X-Ray trace id (<c>X-Amzn-Trace-Id</c>) of the current request, or <c>"Unknown"</c>.
    /// </summary>
    string AmazonTraceId { get; }
}
