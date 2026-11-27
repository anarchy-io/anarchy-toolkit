namespace Anarchy.Toolkit.Bootstrap.Aws.Core.Service;

/// <summary>
/// Exposes the AWS identifiers of the request or invocation currently being processed.
/// </summary>
/// <remarks>
/// Registered by <see cref="Anarchy.Toolkit.Bootstrap.Aws.DependencyInjection.Module.ModuleAwsRequestContext"/> with the
/// <see cref="Anarchy.Toolkit.Bootstrap.Aws.Telemetry.AmazonRequestContextAccessor"/> implementation, whose values come
/// from async-local storage and therefore reflect the current async flow.
/// </remarks>
public interface IAmazonRequestContext
{
    /// <summary>
    /// Gets the AWS X-Ray trace id (<c>X-Amzn-Trace-Id</c>) of the current request, or <c>"Unknown"</c>.
    /// </summary>
    string AmazonTraceId { get; }
}
