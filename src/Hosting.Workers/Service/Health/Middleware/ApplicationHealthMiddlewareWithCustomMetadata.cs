using Anarchy.Toolkit.Core.Health.Checks;
using Anarchy.Toolkit.Core.Health.Service.Health;
using Microsoft.AspNetCore.Http;

namespace Anarchy.Toolkit.Hosting.Workers.Service.Health.Middleware;

/// <summary>
/// ASP.NET Core middleware that handles GET requests to the known health endpoints (/health/instance,
/// /health/services, and /health/status).
/// Build information comes from a custom <see cref="IBuildMetadata"/> supplied at construction.
/// </summary>
/// <remarks>See <see cref="ApplicationHealthMiddlewareBase"/> for the endpoint behavior.</remarks>
public class ApplicationHealthMiddlewareWithCustomMetadata : ApplicationHealthMiddlewareBase
{
    /// <summary>
    /// Create a new instance of <see cref="ApplicationHealthMiddlewareWithCustomMetadata"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="reports">The service health reports to run.</param>
    /// <param name="buildMetadata">The build metadata provider.</param>
    /// <param name="applicationStartTime">The time the application started, used to compute up-time.</param>
    public ApplicationHealthMiddlewareWithCustomMetadata(
        RequestDelegate next,
        IReadOnlyCollection<IHealthCheck> reports,
        IBuildMetadata buildMetadata,
        DateTimeOffset applicationStartTime) : base(next)
    {
        _reports = reports;
        _buildMetadata = buildMetadata;
        _applicationStartTime = applicationStartTime;
    }

    /// <summary>
    /// Handle a HTTP request with custom metadata
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    public Task Invoke(HttpContext context)
    {
        var healthApi = ApplicationHealthApi.WithCustomMetadata(_reports, _buildMetadata, _applicationStartTime);
        return InvokeWithHealthApi(context, healthApi);
    }

    private readonly IReadOnlyCollection<IHealthCheck> _reports;
    private readonly IBuildMetadata _buildMetadata;
    private readonly DateTimeOffset _applicationStartTime;
}
