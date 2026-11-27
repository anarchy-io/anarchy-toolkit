using Anarchy.Toolkit.Core.Health.Checks;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace Anarchy.Toolkit.Runtime.Workers.Service.Health.Middleware;

/// <summary>
/// ASP.NET Core middleware that handles GET requests to the known health endpoints (/health/instance,
/// /health/services, and /health/status).
/// Build information is read from the <c>Properties/build.meta.json</c> resource embedded in an assembly.
/// </summary>
/// <remarks>See <see cref="ApplicationHealthMiddlewareBase"/> for the endpoint behavior.</remarks>
public class ApplicationHealthMiddlewareWithMetadataFromAssembly : ApplicationHealthMiddlewareBase
{
    /// <summary>
    /// Create a new instance of <see cref="ApplicationHealthMiddlewareWithMetadataFromAssembly"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="reports">The service health reports to run.</param>
    /// <param name="assembly">The assembly containing the embedded build metadata.</param>
    /// <param name="applicationStartTime">The time the application started, used to compute up-time.</param>
    public ApplicationHealthMiddlewareWithMetadataFromAssembly(
        RequestDelegate next,
        IReadOnlyCollection<IHealthCheck> reports,
        Assembly assembly,
        DateTimeOffset applicationStartTime) : base(next)
    {
        _reports = reports;
        _assembly = assembly;
        _applicationStartTime = applicationStartTime;
    }

    /// <summary>
    /// Handle a HTTP request with build metadata from the assembly.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    public Task Invoke(HttpContext context)
    {
        var healthApi = ApplicationHealthApi.WithMetadataFromAssembly(_reports, _assembly, _applicationStartTime);
        return InvokeWithHealthApi(context, healthApi);
    }

    private readonly IReadOnlyCollection<IHealthCheck> _reports;
    private readonly Assembly _assembly;
    private readonly DateTimeOffset _applicationStartTime;
}
